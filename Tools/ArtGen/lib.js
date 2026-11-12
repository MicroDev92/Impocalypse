'use strict';
// Shared drawing primitives for the Impocalypse art generator.
//
// Everything is authored on a 1024 canvas, which imports at PPU 1024 as exactly
// one world unit. A tile is seen at ~61px on desktop and ~22px on a phone, so
// one rendered pixel is ~17 source pixels: shapes here are deliberately thick
// and few. See ASSET_BRIEF.md, "Detail floor".

const S = 1024;
const INK = '#1C0F0C'; // warm near-black shared by every outline

// ---------- seeded randomness (so a variant is reproducible) ----------
function rngFrom(seed) {
  let a = seed >>> 0;
  const r = () => {
    a = (a + 0x6D2B79F5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
  r.range = (lo, hi) => lo + (hi - lo) * r();
  r.int = (lo, hi) => Math.floor(r.range(lo, hi + 1));
  r.pick = arr => arr[Math.floor(r() * arr.length)];
  r.sign = () => (r() < 0.5 ? -1 : 1);
  return r;
}

// ---------- paths ----------
function roundRectPath(ctx, x, y, w, h, r) {
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.arcTo(x + w, y, x + w, y + h, r);
  ctx.arcTo(x + w, y + h, x, y + h, r);
  ctx.arcTo(x, y + h, x, y, r);
  ctx.arcTo(x, y, x + w, y, r);
  ctx.closePath();
}

// Smooth closed curve through the midpoints of a point ring.
function smoothClosed(ctx, pts) {
  const n = pts.length;
  const mid = (a, b) => [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2];
  const m0 = mid(pts[n - 1], pts[0]);
  ctx.beginPath();
  ctx.moveTo(m0[0], m0[1]);
  for (let i = 0; i < n; i++) {
    const p = pts[i], m = mid(p, pts[(i + 1) % n]);
    ctx.quadraticCurveTo(p[0], p[1], m[0], m[1]);
  }
  ctx.closePath();
}

// Smooth open curve through a polyline.
function smoothOpen(ctx, pts) {
  ctx.beginPath();
  ctx.moveTo(pts[0][0], pts[0][1]);
  for (let i = 1; i < pts.length - 1; i++) {
    const mx = (pts[i][0] + pts[i + 1][0]) / 2, my = (pts[i][1] + pts[i + 1][1]) / 2;
    ctx.quadraticCurveTo(pts[i][0], pts[i][1], mx, my);
  }
  const l = pts[pts.length - 1];
  ctx.lineTo(l[0], l[1]);
}

// A wobbly organic ring of points - the basis of every hand-drawn shape.
function blobPts(cx, cy, rx, ry, rng, o = {}) {
  const n = o.n || 9, wob = o.wob ?? 0.12, rot = o.rot ?? rng.range(0, Math.PI * 2);
  const pts = [];
  for (let i = 0; i < n; i++) {
    const a = rot + (i / n) * Math.PI * 2;
    const k = 1 + rng.range(-wob, wob);
    pts.push([cx + Math.cos(a) * rx * k, cy + Math.sin(a) * ry * k]);
  }
  return pts;
}

function fillPts(ctx, pts, color) {
  smoothClosed(ctx, pts);
  ctx.fillStyle = color;
  ctx.fill();
}

function strokePts(ctx, pts, width, color = INK) {
  smoothClosed(ctx, pts);
  ctx.lineWidth = width;
  ctx.lineJoin = 'round';
  ctx.strokeStyle = color;
  ctx.stroke();
}

function fillEllipse(ctx, cx, cy, rx, ry, color, rot = 0) {
  ctx.beginPath();
  ctx.ellipse(cx, cy, rx, ry, rot, 0, Math.PI * 2);
  ctx.fillStyle = color;
  ctx.fill();
}

// Flat low-contrast patches: the "painted by hand" unevenness of a ground colour.
function mottle(ctx, rng, colors, count, rmin, rmax) {
  for (let i = 0; i < count; i++) {
    const r = rng.range(rmin, rmax);
    const pts = blobPts(rng.range(0, S), rng.range(0, S), r, r * rng.range(0.6, 1), rng, { n: 8, wob: 0.22 });
    fillPts(ctx, pts, rng.pick(colors));
  }
}

// Angular polyline from a to b: equally spaced points pushed sideways.
function jagged(rng, a, b, segs, amp) {
  const dx = b[0] - a[0], dy = b[1] - a[1];
  const len = Math.hypot(dx, dy) || 1;
  const nx = -dy / len, ny = dx / len;
  const pts = [a];
  let side = rng.sign();
  for (let i = 1; i < segs; i++) {
    const t = i / segs + rng.range(-0.25, 0.25) / segs;
    const off = side * rng.range(0.35, 1) * amp;
    side = -side;
    pts.push([a[0] + dx * t + nx * off, a[1] + dy * t + ny * off]);
  }
  pts.push(b);
  return pts;
}

// Polyline drawn segment by segment so its width can taper.
function taperedStroke(ctx, pts, w0, w1, color, grow = 0, scale = 1) {
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';
  ctx.strokeStyle = color;
  const n = pts.length - 1;
  for (let i = 0; i < n; i++) {
    const w = (w0 + (w1 - w0) * (i / Math.max(1, n - 1))) * scale + grow;
    ctx.lineWidth = Math.max(1, w);
    ctx.beginPath();
    ctx.moveTo(pts[i][0], pts[i][1]);
    ctx.lineTo(pts[i + 1][0], pts[i + 1][1]);
    ctx.stroke();
  }
}

// ---------- the tile plate ----------
// Every terrain is a rounded plate with a thin dark gap around it: neighbouring
// tiles then show a soft dark seam, like the mockup. The rim shading is symmetric
// on purpose - tiles are rotated and mirrored at runtime, so any directional
// light on the plate would point a different way on every tile.
function tileFrame(ctx, paint, o = {}) {
  const inset = o.inset ?? 12, r = o.radius ?? 64;
  ctx.save();
  ctx.fillStyle = o.gap ?? '#150B09';
  ctx.fillRect(0, 0, S, S);

  roundRectPath(ctx, inset, inset, S - 2 * inset, S - 2 * inset, r);
  ctx.clip();
  paint(ctx);

  // inner shade band, then a faint light line inside it
  roundRectPath(ctx, inset, inset, S - 2 * inset, S - 2 * inset, r);
  ctx.lineWidth = 44;
  ctx.strokeStyle = o.rimShade ?? 'rgba(0,0,0,0.16)';
  ctx.stroke();
  const li = inset + 30;
  roundRectPath(ctx, li, li, S - 2 * li, S - 2 * li, r - 22);
  ctx.lineWidth = 7;
  ctx.strokeStyle = o.rimLight ?? 'rgba(255,255,255,0.06)';
  ctx.stroke();
  ctx.restore();

  roundRectPath(ctx, inset, inset, S - 2 * inset, S - 2 * inset, r);
  ctx.lineWidth = o.outline ?? 12;
  ctx.strokeStyle = INK;
  ctx.stroke();
}

// ---------- output helpers ----------
function newCanvas(w, h = w) {
  const c = document.createElement('canvas');
  c.width = w;
  c.height = h;
  return c;
}

// Halve repeatedly, then one final resample: close to what mipmapped bilinear
// filtering does in the engine, and far better than one giant downscale.
function mipDown(src, target) {
  let cur = src;
  while (cur.width / 2 >= target * 1.5) {
    const next = newCanvas(cur.width / 2, cur.height / 2);
    const c = next.getContext('2d');
    c.imageSmoothingQuality = 'high';
    c.drawImage(cur, 0, 0, next.width, next.height);
    cur = next;
  }
  const out = newCanvas(target);
  const c = out.getContext('2d');
  c.imageSmoothingQuality = 'high';
  c.drawImage(cur, 0, 0, target, target);
  return out;
}

async function saveCanvas(canvas, name) {
  const blob = await new Promise(res => canvas.toBlob(res, 'image/png'));
  const r = await fetch('/save?name=' + encodeURIComponent(name), { method: 'POST', body: blob });
  if (!r.ok) throw new Error('save failed: ' + name);
}
