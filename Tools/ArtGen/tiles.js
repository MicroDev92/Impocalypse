'use strict';
// Terrain painters. Each draws one 1024 tile; variants come from a seed and a
// "kind" so layouts differ on purpose rather than by luck.
//
// Runtime transforms these must survive (GridGenerator.ApplyTerrainVariation):
//   Ashlands, Styx  - rotated in 90 degree steps (and Ashlands mirrored)
//   Grove, Peaks    - mirrored only, so "up" stays up
//   Field           - untouched
// So Ashlands and Styx carry no directional light at all.

// =====================================================================
// ASHLANDS - 40% of the board. Quiet ground: thick, few cracks.
// =====================================================================
const ASH = {
  base: '#B65F2F', light: '#BE6835', dark: '#AC582B',
  lip: '#CB7841', crack: '#5A2916', core: '#35170C',
  stone: '#8E4A28', stoneHi: '#BC7242', tick: '#9B4D26',
};

function ashCracks(rng, kind) {
  const J = (p, d = 36) => [p[0] + rng.range(-d, d), p[1] + rng.range(-d, d)];
  const lines = [];
  const add = (a, b, segs, amp, w0, w1) => {
    const pts = jagged(rng, a, b, segs, amp);
    lines.push({ pts, w0, w1 });
    return pts;
  };
  if (kind === 0) {            // a Y across the plate
    const m = add(J([170, 230]), J([520, 540]), 4, 44, 46, 36);
    const j = m[m.length - 1];
    add(j, J([850, 400]), 3, 40, 36, 16);
    add(j, J([630, 880]), 3, 40, 36, 16);
  } else if (kind === 1) {     // one long diagonal with a twig
    const m = add(J([130, 800]), J([890, 270]), 6, 50, 44, 20);
    add(m[3], J([780, 750]), 3, 34, 30, 14);
  } else if (kind === 2) {     // down from an edge, forking low
    const m = add(J([520, 30], 16), J([470, 560]), 4, 42, 46, 34);
    const j = m[m.length - 1];
    add(j, J([170, 730]), 3, 36, 34, 15);
    add(j, J([810, 800]), 3, 36, 34, 15);
  } else {                     // two short, separate cracks
    add(J([140, 310]), J([500, 450]), 3, 40, 42, 18);
    add(J([540, 640]), J([900, 800]), 3, 40, 42, 18);
  }
  return lines;
}

function paintAshlands(ctx, rng, kind) {
  ctx.fillStyle = ASH.base;
  ctx.fillRect(0, 0, S, S);
  mottle(ctx, rng, [ASH.light, ASH.dark], 8, 110, 230);

  // scuff marks: invisible at board zoom, texture when zoomed in
  ctx.lineCap = 'round';
  for (let i = 0; i < 16; i++) {
    const x = rng.range(90, S - 90), y = rng.range(90, S - 90), a = rng.range(0, Math.PI), l = rng.range(14, 30);
    ctx.beginPath();
    ctx.moveTo(x - Math.cos(a) * l, y - Math.sin(a) * l);
    ctx.lineTo(x + Math.cos(a) * l, y + Math.sin(a) * l);
    ctx.lineWidth = 9;
    ctx.strokeStyle = ASH.tick;
    ctx.stroke();
  }

  const lines = ashCracks(rng, kind);
  for (const l of lines) taperedStroke(ctx, l.pts, l.w0, l.w1, ASH.lip, 22);   // pale dried lip
  for (const l of lines) taperedStroke(ctx, l.pts, l.w0, l.w1, ASH.crack);
  for (const l of lines) taperedStroke(ctx, l.pts, l.w0, l.w1, ASH.core, 0, 0.36);

  const stones = kind === 3 ? 5 : rng.int(2, 3);
  for (let i = 0; i < stones; i++) {
    const r = rng.range(20, 34);
    const x = rng.range(150, S - 150), y = rng.range(150, S - 150);
    const pts = blobPts(x, y, r, r * rng.range(0.7, 0.95), rng, { n: 7, wob: 0.16 });
    strokePts(ctx, pts, 16, ASH.crack);
    fillPts(ctx, pts, ASH.stone);
    fillEllipse(ctx, x, y - r * 0.18, r * 0.42, r * 0.24, ASH.stoneHi);
  }
}

// =====================================================================
// THE STYX - a river of souls, seen from above, so nothing has an "up".
// =====================================================================
const STYX = {
  // Deliberately deeper than the mockup's water: the teal player's creature and
  // ownership wash are light teal, and on a bright Styx they vanished into it.
  base: '#1F8E78', dark: '#197C68', light: '#279C84',
  under: '#12604F', ribbon: '#62D4B8', hi: '#BDF5E6',
  soul: '#E4FCF6', soulInk: '#0D4A3D', eye: '#0C3A31',
};

function ribbonPts(rng, cx, cy, len, ang) {
  const pts = [], n = 6;
  const amp = rng.range(46, 74) * rng.sign();
  const ca = Math.cos(ang), sa = Math.sin(ang);
  for (let i = 0; i <= n; i++) {
    const t = i / n - 0.5;
    const x = t * len, y = Math.sin(t * Math.PI * 1.7) * amp;
    pts.push([cx + x * ca - y * sa, cy + x * sa + y * ca]);
  }
  // finish in a curl
  const end = pts[pts.length - 1], s = amp > 0 ? -1 : 1, R = rng.range(56, 72);
  const c = [end[0] - sa * s * R, end[1] + ca * s * R];
  const phi0 = Math.atan2(end[1] - c[1], end[0] - c[0]);
  for (let k = 1; k <= 6; k++) {
    const phi = phi0 + s * k * 0.82, rr = R * (1 - k * 0.125);
    pts.push([c[0] + Math.cos(phi) * rr, c[1] + Math.sin(phi) * rr]);
  }
  return pts;
}

function drawSoul(ctx, rng, x, y, ang, s) {
  const dx = Math.cos(ang), dy = Math.sin(ang);
  const tail = [];
  const sway = rng.range(20, 34) * rng.sign();
  for (let i = 0; i <= 6; i++) {
    const t = i / 6, d = t * 190 * s, lat = Math.sin(t * 3.4) * sway * s;
    tail.push([x - dx * d - dy * lat, y - dy * d + dx * lat]);
  }
  taperedStroke(ctx, tail, 92 * s, 12 * s, STYX.soulInk, 20);
  fillEllipse(ctx, x, y, 50 * s + 10, 50 * s + 10, STYX.soulInk);
  taperedStroke(ctx, tail, 92 * s, 12 * s, STYX.soul);
  fillEllipse(ctx, x, y, 50 * s, 50 * s, STYX.soul);
  // eyes and a little "o", looking the way it swims
  for (const side of [-1, 1]) {
    fillEllipse(ctx, x + dx * 8 * s - dy * side * 19 * s, y + dy * 8 * s + dx * side * 19 * s, 12 * s, 9 * s, STYX.eye, ang);
  }
  fillEllipse(ctx, x + dx * 30 * s, y + dy * 30 * s, 6 * s, 6 * s, STYX.eye);
}

function placePoints(rng, count, minDist, lo, hi, avoid = []) {
  const out = [];
  let guard = 0;
  while (out.length < count && guard++ < 400) {
    const p = [rng.range(lo, hi), rng.range(lo, hi)];
    if ([...out, ...avoid].every(q => Math.hypot(q[0] - p[0], q[1] - p[1]) >= minDist)) out.push(p);
  }
  return out;
}

function paintStyx(ctx, rng, kind) {
  ctx.fillStyle = STYX.base;
  ctx.fillRect(0, 0, S, S);
  mottle(ctx, rng, [STYX.light, STYX.dark], 8, 110, 220);

  const centres = placePoints(rng, 3, 340, 230, S - 230);
  const ribbons = centres.map(c => ribbonPts(rng, c[0], c[1], rng.range(330, 430), rng.range(0, Math.PI * 2)));
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';
  for (const [w, col] of [[78, STYX.under], [56, STYX.ribbon], [15, STYX.hi]]) {
    for (const r of ribbons) {
      smoothOpen(ctx, r);
      ctx.lineWidth = w;
      ctx.strokeStyle = col;
      ctx.stroke();
    }
  }

  const souls = placePoints(rng, kind === 2 ? 1 : 2, 300, 200, S - 200);
  for (const p of souls) drawSoul(ctx, rng, p[0], p[1], rng.range(0, Math.PI * 2), rng.range(0.82, 1));

  for (let i = 0; i < 7; i++) {
    const r = rng.range(10, 20);
    ctx.beginPath();
    ctx.arc(rng.range(100, S - 100), rng.range(100, S - 100), r, 0, Math.PI * 2);
    ctx.lineWidth = 7;
    ctx.strokeStyle = STYX.hi;
    ctx.stroke();
  }
}

// =====================================================================
// GLOOMWOOD GROVE - dark ground, loud pink canopies. Mirrored only.
// =====================================================================
const GROVE = {
  base: '#3A2041', dark: '#2E1835', light: '#472A50',
  trunk: '#2A1630', trunkHi: '#4B2C56',
  leaf: '#E4458B', leafHi: '#F884B6', leafLo: '#B32E6D',
  eye: '#FFD23F', petal: '#C43D7C',
  cap: '#F06AA6', stem: '#DCC6DB',
};

function drawTree(ctx, rng, x, y, s) {
  const ow = 15;
  fillEllipse(ctx, x, y + 8 * s, 130 * s, 36 * s, 'rgba(0,0,0,0.32)');

  const lean = rng.range(-26, 26) * s, h = rng.range(215, 250) * s;
  const trunk = [
    [x - 70 * s, y + 4 * s], [x - 34 * s, y - 46 * s], [x - 27 * s + lean * 0.5, y - h * 0.56],
    [x - 34 * s + lean, y - h], [x + 34 * s + lean, y - h], [x + 26 * s + lean * 0.5, y - h * 0.56],
    [x + 36 * s, y - 46 * s], [x + 72 * s, y + 4 * s], [x + 18 * s, y + 16 * s], [x - 20 * s, y + 16 * s],
  ];
  strokePts(ctx, trunk, ow * 2);
  fillPts(ctx, trunk, GROVE.trunk);
  ctx.save();
  smoothClosed(ctx, trunk);
  ctx.clip();
  fillEllipse(ctx, x + lean * 0.5, y - h * 0.5, 13 * s, h * 0.5, GROVE.trunkHi);
  ctx.restore();

  // a carved face: gone at board zoom, a reward for zooming in
  const fx = x + lean * 0.55, fy = y - h * 0.5;
  for (const side of [-1, 1]) fillEllipse(ctx, fx + side * 13 * s, fy, 8 * s, 5.5 * s, GROVE.eye, side * 0.5);
  ctx.beginPath();
  ctx.moveTo(fx - 14 * s, fy + 22 * s);
  ctx.lineTo(fx - 6 * s, fy + 30 * s);
  ctx.lineTo(fx, fy + 22 * s);
  ctx.lineTo(fx + 6 * s, fy + 30 * s);
  ctx.lineTo(fx + 14 * s, fy + 22 * s);
  ctx.lineWidth = 5 * s;
  ctx.lineJoin = 'round';
  ctx.lineCap = 'round';
  ctx.strokeStyle = GROVE.eye;
  ctx.stroke();

  // canopy: outline every clump first, then fill, so the clumps share one silhouette
  const cx = x + lean, cy = y - h - 58 * s;
  const clumps = [[-100, 6, 100], [100, 0, 100], [-44, -66, 112], [52, -74, 108], [0, 4, 118]].map(([ox, oy, r]) => ({
    x: cx + ox * s + rng.range(-10, 10) * s, y: cy + oy * s + rng.range(-10, 10) * s, r: r * s * rng.range(0.92, 1.06),
  }));
  for (const c of clumps) c.pts = blobPts(c.x, c.y, c.r, c.r * 0.92, rng, { n: 9, wob: 0.1 });
  for (const c of clumps) strokePts(ctx, c.pts, ow * 2);
  for (const c of clumps) {
    fillPts(ctx, c.pts, GROVE.leafLo);
    ctx.save();
    smoothClosed(ctx, c.pts);
    ctx.clip();
    fillEllipse(ctx, c.x, c.y - c.r * 0.16, c.r * 1.02, c.r * 0.92, GROVE.leaf);
    fillEllipse(ctx, c.x - c.r * 0.08, c.y - c.r * 0.42, c.r * 0.5, c.r * 0.3, GROVE.leafHi);
    ctx.restore();
  }
}

function drawMushroom(ctx, rng, x, y, s) {
  const stem = [[x - 13 * s, y], [x - 11 * s, y - 40 * s], [x + 11 * s, y - 40 * s], [x + 13 * s, y]];
  ctx.beginPath();
  ctx.moveTo(stem[0][0], stem[0][1]);
  for (const p of stem.slice(1)) ctx.lineTo(p[0], p[1]);
  ctx.closePath();
  ctx.lineJoin = 'round';
  ctx.lineWidth = 20;
  ctx.strokeStyle = INK;
  ctx.stroke();
  ctx.fillStyle = GROVE.stem;
  ctx.fill();
  ctx.beginPath();
  ctx.ellipse(x, y - 40 * s, 46 * s, 36 * s, 0, Math.PI, 0);
  ctx.closePath();
  ctx.lineWidth = 20;
  ctx.strokeStyle = INK;
  ctx.stroke();
  ctx.fillStyle = GROVE.cap;
  ctx.fill();
  fillEllipse(ctx, x - 16 * s, y - 56 * s, 8 * s, 6 * s, GROVE.stem);
  fillEllipse(ctx, x + 14 * s, y - 50 * s, 6 * s, 5 * s, GROVE.stem);
}

function paintGrove(ctx, rng, kind) {
  ctx.fillStyle = GROVE.base;
  ctx.fillRect(0, 0, S, S);
  mottle(ctx, rng, [GROVE.light, GROVE.dark], 9, 100, 210);
  for (let i = 0; i < 12; i++) {
    fillEllipse(ctx, rng.range(90, S - 90), rng.range(90, S - 90), rng.range(10, 17), rng.range(6, 10), GROVE.petal, rng.range(0, Math.PI));
  }

  if (kind === 0) {
    drawTree(ctx, rng, 300, 560, 0.86);
    drawTree(ctx, rng, 735, 540, 0.82);
    drawTree(ctx, rng, 520, 900, 1.0);
  } else {
    drawTree(ctx, rng, 690, 560, 0.9);
    drawTree(ctx, rng, 330, 880, 1.02);
    drawMushroom(ctx, rng, 720, 860, 1.25);
    drawMushroom(ctx, rng, 820, 900, 0.95);
    drawMushroom(ctx, rng, 250, 380, 1.0);
  }
}

// =====================================================================
// BLOODROOT FIELD - a tended plot. Never rotated or mirrored.
// =====================================================================
const FIELD = {
  soil: '#55301F', soilDark: '#472819', soilLight: '#613824',
  bed: '#6B3E28', bedHi: '#8A5433', furrow: '#4B2A1B',
  bulb: '#D8362B', bulbHi: '#F47466', bulbLo: '#A6231F',
  leaf: '#7A9632', leafLo: '#506B22',
};

function drawLeaf(ctx, x, y, ang, len, wid) {
  const ca = Math.cos(ang), sa = Math.sin(ang);
  const P = (u, v) => [x + ca * u - sa * v, y + sa * u + ca * v];
  const tip = P(len, 0), l = P(len * 0.45, -wid), r = P(len * 0.45, wid);
  ctx.beginPath();
  ctx.moveTo(x, y);
  ctx.quadraticCurveTo(l[0], l[1], tip[0], tip[1]);
  ctx.quadraticCurveTo(r[0], r[1], x, y);
  ctx.closePath();
  ctx.lineJoin = 'round';
  ctx.lineWidth = 20;
  ctx.strokeStyle = INK;
  ctx.stroke();
  ctx.fillStyle = FIELD.leaf;
  ctx.fill();
  ctx.beginPath();
  ctx.moveTo(x, y);
  const m = P(len * 0.78, 0);
  ctx.lineTo(m[0], m[1]);
  ctx.lineWidth = 6;
  ctx.strokeStyle = FIELD.leafLo;
  ctx.stroke();
}

function drawBloodroot(ctx, rng, x, y, s) {
  fillEllipse(ctx, x, y + 44 * s, 86 * s, 26 * s, FIELD.furrow);
  const top = y - 34 * s;
  const spread = rng.range(0.5, 0.68);
  drawLeaf(ctx, x - 8 * s, top, -Math.PI / 2 - spread, rng.range(96, 116) * s, 30 * s);
  drawLeaf(ctx, x + 8 * s, top, -Math.PI / 2 + spread, rng.range(96, 116) * s, 30 * s);
  drawLeaf(ctx, x, top, -Math.PI / 2 + rng.range(-0.1, 0.1), rng.range(120, 140) * s, 32 * s);

  const pts = blobPts(x, y + 4 * s, 68 * s, 58 * s, rng, { n: 9, wob: 0.07 });
  strokePts(ctx, pts, 28);
  fillPts(ctx, pts, FIELD.bulb);
  ctx.save();
  smoothClosed(ctx, pts);
  ctx.clip();
  fillEllipse(ctx, x + 6 * s, y + 52 * s, 80 * s, 34 * s, FIELD.bulbLo);
  fillEllipse(ctx, x - 22 * s, y - 20 * s, 24 * s, 13 * s, FIELD.bulbHi, -0.5);
  ctx.restore();
}

function paintField(ctx, rng, kind) {
  ctx.fillStyle = FIELD.soil;
  ctx.fillRect(0, 0, S, S);
  mottle(ctx, rng, [FIELD.soilLight, FIELD.soilDark], 7, 100, 200);

  // the raised bed
  roundRectPath(ctx, 92, 92, S - 184, S - 184, 78);
  ctx.lineWidth = 30;
  ctx.strokeStyle = INK;
  ctx.stroke();
  ctx.fillStyle = FIELD.bed;
  ctx.fill();
  roundRectPath(ctx, 118, 118, S - 236, S - 236, 60);
  ctx.lineWidth = 10;
  ctx.strokeStyle = FIELD.bedHi;
  ctx.stroke();

  // furrows between the rows
  ctx.lineCap = 'round';
  for (const fy of [250, 548, 846]) {
    const pts = [];
    for (let i = 0; i <= 6; i++) pts.push([170 + i * (S - 340) / 6, fy + rng.range(-10, 10)]);
    smoothOpen(ctx, pts);
    ctx.lineWidth = 24;
    ctx.strokeStyle = FIELD.furrow;
    ctx.stroke();
  }

  // kind 0: a full 3x2 plot. kind 1: one plant lost to a skull on a stake, so
  // neighbouring plots do not read as the same tile stamped twice.
  const xs = [292, 512, 732], ys = [400, 700];
  for (const py of ys) {
    for (const px of xs) {
      if (kind === 1 && py === ys[1] && px === xs[2]) {
        drawSkullStake(ctx, rng, px + 6, py - 6, 1);
        continue;
      }
      const s = kind === 0 ? rng.range(0.92, 1.04) : rng.range(0.84, 1.08);
      drawBloodroot(ctx, rng, px + rng.range(-16, 16), py + rng.range(-12, 12), s);
    }
  }
}

function drawSkullStake(ctx, rng, x, y, s) {
  fillEllipse(ctx, x, y + 58 * s, 60 * s, 20 * s, FIELD.furrow);
  ctx.lineCap = 'round';
  for (const [w, col] of [[38 * s, INK], [18 * s, '#9C6C40']]) {
    ctx.beginPath();
    ctx.moveTo(x, y + 56 * s);
    ctx.lineTo(x, y - 20 * s);
    ctx.lineWidth = w;
    ctx.strokeStyle = col;
    ctx.stroke();
  }
  const bone = '#F0E4CC', boneLo = '#CDBB9C';
  // jaw first, cranium over it
  roundRectPath(ctx, x - 30 * s, y - 44 * s, 60 * s, 40 * s, 12 * s);
  ctx.lineWidth = 22;
  ctx.strokeStyle = INK;
  ctx.stroke();
  ctx.fillStyle = boneLo;
  ctx.fill();
  const head = blobPts(x, y - 78 * s, 60 * s, 52 * s, rng, { n: 9, wob: 0.05 });
  strokePts(ctx, head, 22);
  fillPts(ctx, head, bone);
  for (const side of [-1, 1]) fillEllipse(ctx, x + side * 23 * s, y - 76 * s, 14 * s, 17 * s, INK, side * 0.2);
  fillEllipse(ctx, x, y - 50 * s, 6 * s, 9 * s, INK);
  ctx.beginPath();
  for (const tx of [-12, 0, 12]) {
    ctx.moveTo(x + tx * s, y - 26 * s);
    ctx.lineTo(x + tx * s, y - 10 * s);
  }
  ctx.lineWidth = 5 * s;
  ctx.strokeStyle = INK;
  ctx.stroke();
}

// =====================================================================
// BRIMSTONE PEAKS - the darkest terrain, lit only by its own lava.
// =====================================================================
const PEAKS = {
  base: '#2A2431', dark: '#221D28', light: '#342D3D',
  lit: '#74677F', rim: '#978BA3', face: '#40374A', deep: '#2E2736',
  lava: '#FF6B1A', core: '#FFD84A', ember: '#FF8A2A',
};

function polyPath(ctx, pts) {
  ctx.beginPath();
  ctx.moveTo(pts[0][0], pts[0][1]);
  for (const p of pts.slice(1)) ctx.lineTo(p[0], p[1]);
  ctx.closePath();
}

function drawPeak(ctx, rng, cx, baseY, w, h) {
  const j = d => rng.range(-d, d);
  const apex = [cx + j(w * 0.07), baseY - h];
  const L = [cx - w / 2, baseY], R = [cx + w / 2, baseY];
  const l1 = [cx - w * 0.37 + j(12), baseY - h * 0.36], l2 = [cx - w * 0.17 + j(10), baseY - h * 0.72];
  const r1 = [cx + w * 0.37 + j(12), baseY - h * 0.34], r2 = [cx + w * 0.18 + j(10), baseY - h * 0.70];
  const foot = [cx + j(w * 0.06), baseY + 20];
  const outline = [L, l1, l2, apex, r2, r1, R, [cx + w * 0.24, baseY + 16], foot, [cx - w * 0.22, baseY + 14]];
  const ridge = [apex, [apex[0] + w * 0.06, baseY - h * 0.66], [apex[0] - w * 0.05, baseY - h * 0.38], foot];

  fillEllipse(ctx, cx, baseY + 14, w * 0.56, 30, 'rgba(0,0,0,0.35)');
  polyPath(ctx, outline);
  ctx.lineJoin = 'round';
  ctx.lineWidth = 30;
  ctx.strokeStyle = INK;
  ctx.stroke();
  ctx.fillStyle = PEAKS.face;
  ctx.fill();
  // the shadowed face darkens towards its foot
  ctx.save();
  polyPath(ctx, outline);
  ctx.clip();
  fillEllipse(ctx, cx + w * 0.3, baseY + 6, w * 0.34, h * 0.3, PEAKS.deep);
  ctx.restore();

  polyPath(ctx, [L, l1, l2, ...ridge, [cx - w * 0.22, baseY + 14]]);
  ctx.fillStyle = PEAKS.lit;
  ctx.fill();

  // a pale edge and two facet cuts give the lit face some rock to it
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';
  ctx.beginPath();
  ctx.moveTo(l1[0] + w * 0.075, l1[1] + h * 0.03);
  ctx.lineTo(l2[0] + w * 0.07, l2[1] + h * 0.04);
  ctx.lineTo(apex[0] - w * 0.005, apex[1] + h * 0.1);
  ctx.lineWidth = 12;
  ctx.strokeStyle = PEAKS.rim;
  ctx.stroke();
  ctx.beginPath();
  ctx.moveTo(l2[0] + w * 0.13, l2[1] + h * 0.16);
  ctx.lineTo(l2[0] + w * 0.07, l2[1] + h * 0.32);
  ctx.moveTo(l1[0] + w * 0.2, l1[1] + h * 0.1);
  ctx.lineTo(l1[0] + w * 0.15, l1[1] + h * 0.24);
  ctx.lineWidth = 11;
  ctx.strokeStyle = PEAKS.face;
  ctx.stroke();

  // lava running down the ridge, glowing
  const lava = [apex, ridge[1], ridge[2], [ridge[2][0] + w * 0.07, baseY - h * 0.2]];
  ctx.save();
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';
  ctx.beginPath();
  ctx.moveTo(lava[0][0], lava[0][1] + 8);
  for (const p of lava.slice(1)) ctx.lineTo(p[0], p[1]);
  ctx.shadowColor = PEAKS.lava;
  ctx.shadowBlur = 36;
  ctx.lineWidth = 24;
  ctx.strokeStyle = PEAKS.lava;
  ctx.stroke();
  ctx.shadowBlur = 0;
  ctx.lineWidth = 9;
  ctx.strokeStyle = PEAKS.core;
  ctx.stroke();
  ctx.restore();
}

function drawRock(ctx, rng, x, y, r) {
  const pts = [[x - r, y], [x - r * 0.5, y - r * 0.8], [x + r * 0.3, y - r], [x + r, y - r * 0.1], [x + r * 0.6, y + r * 0.3], [x - r * 0.6, y + r * 0.3]];
  polyPath(ctx, pts);
  ctx.lineJoin = 'round';
  ctx.lineWidth = 20;
  ctx.strokeStyle = INK;
  ctx.stroke();
  ctx.fillStyle = PEAKS.face;
  ctx.fill();
  polyPath(ctx, [pts[0], pts[1], pts[2], [x, y + r * 0.3], pts[5]]);
  ctx.fillStyle = PEAKS.lit;
  ctx.fill();
}

function paintPeaks(ctx, rng, kind) {
  ctx.fillStyle = PEAKS.base;
  ctx.fillRect(0, 0, S, S);
  mottle(ctx, rng, [PEAKS.light, PEAKS.dark], 8, 100, 210);
  for (let i = 0; i < 9; i++) fillEllipse(ctx, rng.range(90, S - 90), rng.range(90, S - 90), rng.range(6, 10), rng.range(6, 10), PEAKS.ember);

  if (kind === 0) {
    drawPeak(ctx, rng, 300, 610, 330, 400);
    drawPeak(ctx, rng, 745, 590, 300, 360);
    drawPeak(ctx, rng, 520, 890, 440, 540);
    drawRock(ctx, rng, 200, 860, 50);
    drawRock(ctx, rng, 850, 850, 44);
  } else {
    drawPeak(ctx, rng, 640, 560, 380, 420);
    drawPeak(ctx, rng, 330, 880, 430, 560);
    drawPeak(ctx, rng, 760, 900, 300, 320);
    drawRock(ctx, rng, 240, 330, 46);
  }
}

// =====================================================================
// The set. type letters: P plains/ashlands, W styx, G grove, F field, M peaks.
// =====================================================================
const TILESET = [
  { name: 'ashlands_a', type: 'P', paint: c => paintAshlands(c, rngFrom(1101), 0) },
  { name: 'ashlands_b', type: 'P', paint: c => paintAshlands(c, rngFrom(1202), 1) },
  { name: 'ashlands_c', type: 'P', paint: c => paintAshlands(c, rngFrom(1303), 2) },
  { name: 'ashlands_d', type: 'P', paint: c => paintAshlands(c, rngFrom(1404), 3) },
  { name: 'styx_a', type: 'W', paint: c => paintStyx(c, rngFrom(2101), 0) },
  { name: 'styx_b', type: 'W', paint: c => paintStyx(c, rngFrom(2202), 1) },
  { name: 'styx_c', type: 'W', paint: c => paintStyx(c, rngFrom(2303), 2) },
  { name: 'grove_a', type: 'G', paint: c => paintGrove(c, rngFrom(3101), 0) },
  { name: 'grove_b', type: 'G', paint: c => paintGrove(c, rngFrom(3202), 1) },
  { name: 'field_a', type: 'F', paint: c => paintField(c, rngFrom(4101), 0) },
  { name: 'field_b', type: 'F', paint: c => paintField(c, rngFrom(4202), 1) },
  { name: 'peaks_a', type: 'M', paint: c => paintPeaks(c, rngFrom(5101), 0) },
  { name: 'peaks_b', type: 'M', paint: c => paintPeaks(c, rngFrom(5202), 1) },
];
