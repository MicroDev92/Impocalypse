'use strict';
// Building markers and card art. A marker is drawn over a terrain tile at 0.55
// of the cell: ~34px on desktop, ~12px on a phone. So each building is one
// blunt silhouette (keep / barn / awning / tent / ring) with a single
// bright accent, and nothing thinner than the outline.

const BS = 1024;
const BO = 34; // outline width
const BC = {
  stone: '#8A7F92', stoneLo: '#5B5263', stoneHi: '#B4A9BC',
  ember: '#FF8A2A', emberHi: '#FFD36B',
  red: '#C8322F', redLo: '#8E2320', redHi: '#E8573F',
  wood: '#9A5A3A', woodLo: '#4A2C20', woodHi: '#C98A5A',
  bone: '#F3E3C3', gold: '#F2B53C',
  silk: '#A45BD6', silkHi: '#CFA0F0', portal: '#7A2FB8', portalHi: '#E08AFF',
  sack: '#D9A066', sackLo: '#B07A45', dark: '#24120F',
};

function bothSides(ctx, fn) {
  fn();
  ctx.save();
  ctx.translate(BS, 0);
  ctx.scale(-1, 1);
  fn();
  ctx.restore();
}
function post(ctx, pts, color, w) {
  line(ctx, pts, INK, w + 44);
  line(ctx, pts, color, w);
}
const shape = build => ctx => build(ctx);

// Citadel parts. Strength is the number of towers: one, two, or the full keep
// with a tower either side.
function citadelTower(ctx, x, w, top, slit) {
  ink(ctx, poly([[x - 10, 890], [x + 10, top + 110], [x + w - 10, top + 110], [x + w + 10, 890]]), BC.stone, BO);
  const m = w / 5, l = x - 30, r = x + w + 30, n = top + 56;
  ink(ctx, poly([[l, top + 126], [l, top], [l + m + 20, top], [l + m + 20, n], [l + 2 * m + 20, n], [l + 2 * m + 20, top], [r - 2 * m - 20, top], [r - 2 * m - 20, n], [r - m - 20, n], [r - m - 20, top], [r, top], [r, top + 126]]), BC.stoneHi, BO);
  if (slit) ink(ctx, rrect(x + w / 2 - 28, top + 200, 56, 110, 24), BC.ember, 20);
}
function citadelGate(ctx, cx, w, top) {
  ink(ctx, shape(c => {
    c.moveTo(cx - w / 2, 890);
    c.lineTo(cx - w / 2, top + w / 2);
    c.bezierCurveTo(cx - w / 2, top - 40, cx + w / 2, top - 40, cx + w / 2, top + w / 2);
    c.lineTo(cx + w / 2, 890);
    c.closePath();
  }), BC.ember, 28);
  const v = w / 2 - 50;
  ink(ctx, shape(c => {
    c.moveTo(cx - v, 876);
    c.lineTo(cx - v, top + w / 2 + 30);
    c.bezierCurveTo(cx - v, top + 50, cx + v, top + 50, cx + v, top + w / 2 + 30);
    c.lineTo(cx + v, 876);
    c.closePath();
  }), BC.emberHi, 0);
}
function citadelFlag(ctx, x, y) {
  post(ctx, [[x, y + 130], [x, y]], BC.woodLo, 8);
  ink(ctx, poly([[x, y - 10], [x + 150, y + 34], [x, y + 80]]), BC.red, 26);
}

const BUILDINGS = {
  citadel1(ctx) {                       // strength 1: a lone tower
    citadelFlag(ctx, 512, 150);
    citadelTower(ctx, 352, 320, 290, false);
    citadelGate(ctx, 512, 180, 600);
  },
  citadel2(ctx) {                       // strength 2: two towers and a gatehouse
    citadelFlag(ctx, 290, 190);
    citadelFlag(ctx, 734, 190);
    ink(ctx, poly([[380, 890], [380, 560], [644, 560], [644, 890]]), BC.stoneLo, BO);
    bothSides(ctx, () => citadelTower(ctx, 160, 260, 330, true));
    citadelGate(ctx, 512, 170, 640);
  },
  // strength 3: the full keep
  citadel(ctx) {
    post(ctx, [[512, 200], [512, 70]], BC.woodLo, 8);
    ink(ctx, poly([[512, 60], [668, 104], [512, 150]]), BC.red, 26);
    bothSides(ctx, () => {
      ink(ctx, poly([[130, 890], [150, 470], [350, 470], [370, 890]]), BC.stone, BO);
      ink(ctx, poly([[108, 486], [108, 360], [182, 360], [182, 412], [214, 412], [214, 360], [286, 360], [286, 412], [318, 412], [318, 360], [392, 360], [392, 486]]), BC.stoneHi, BO);
      ink(ctx, rrect(222, 570, 56, 110, 24), BC.ember, 20);
    });
    ink(ctx, poly([[330, 890], [330, 330], [694, 330], [694, 890]]), BC.stone, BO);
    ink(ctx, poly([[298, 344], [298, 190], [392, 190], [392, 252], [466, 252], [466, 190], [558, 190], [558, 252], [632, 252], [632, 190], [726, 190], [726, 344]]), BC.stoneHi, BO);
    ink(ctx, shape(c => {
      c.moveTo(412, 890);
      c.lineTo(412, 690);
      c.bezierCurveTo(412, 540, 612, 540, 612, 690);
      c.lineTo(612, 890);
      c.closePath();
    }), BC.ember, 28);
    ink(ctx, shape(c => {
      c.moveTo(462, 876);
      c.lineTo(462, 720);
      c.bezierCurveTo(462, 640, 562, 640, 562, 720);
      c.lineTo(562, 876);
      c.closePath();
    }), BC.emberHi, 0);
  },

  farm(ctx) {
    bothSides(ctx, () => ink(ctx, poly([[468, 262], [392, 110], [524, 214]]), BC.bone, BO));
    ink(ctx, poly([[214, 884], [214, 520], [810, 520], [810, 884]]), BC.wood, BO);
    line(ctx, [[360, 560], [360, 860]], BC.woodLo, 14);
    line(ctx, [[664, 560], [664, 860]], BC.woodLo, 14);
    ink(ctx, rrect(428, 630, 168, 254, 22), BC.woodLo, 26);
    line(ctx, [[444, 650], [580, 866]], BC.woodHi, 18);
    line(ctx, [[580, 650], [444, 866]], BC.woodHi, 18);
    ink(ctx, poly([[122, 548], [512, 196], [902, 548]]), BC.red, BO);
    line(ctx, [[512, 262], [250, 500]], BC.redHi, 22);
    ink(ctx, circle(512, 424, 46), BC.dark, 20);
  },

  bazaar(ctx) {
    post(ctx, [[254, 880], [254, 470]], BC.wood, 22);
    post(ctx, [[770, 880], [770, 470]], BC.wood, 22);
    ink(ctx, rrect(184, 676, 656, 210, 26), BC.wood, BO);
    ink(ctx, poly([[398, 676], [626, 676], [626, 824], [512, 778], [398, 824]]), BC.red, 26);
    ink(ctx, circle(300, 640, 46), BC.ember, 24);
    ink(ctx, circle(724, 636, 48), '#C2356E', 24);
    ink(ctx, poly([[512, 262], [512, 128], [612, 174], [512, 214]]), BC.gold, 24);
    ink(ctx, poly([[108, 506], [224, 250], [800, 250], [916, 506]]), BC.silk, BO);
    ctx.save();
    ctx.beginPath(); poly([[108, 506], [224, 250], [800, 250], [916, 506]])(ctx); ctx.clip();
    [[330, 250, 442, 250, 398, 506, 262, 506], [582, 250, 694, 250, 762, 506, 626, 506]].forEach(q => {
      ctx.beginPath();
      ctx.moveTo(q[0], q[1]); ctx.lineTo(q[2], q[3]); ctx.lineTo(q[4], q[5]); ctx.lineTo(q[6], q[7]);
      ctx.closePath();
      ctx.fillStyle = BC.silkHi;
      ctx.fill();
    });
    ctx.restore();
    ink(ctx, poly([[108, 506], [224, 250], [800, 250], [916, 506]]), null, BO);
  },

  warcamp(ctx) {
    post(ctx, [[176, 884], [176, 130]], BC.wood, 16);
    ink(ctx, poly([[176, 130], [420, 190], [176, 286]]), BC.ember, 26);
    post(ctx, [[508, 350], [628, 190]], BC.wood, 14);
    post(ctx, [[612, 350], [492, 190]], BC.wood, 14);
    ink(ctx, poly([[226, 884], [560, 290], [894, 884]]), BC.red, BO);
    line(ctx, [[560, 350], [330, 800]], BC.redHi, 22);
    ink(ctx, poly([[462, 884], [560, 590], [658, 884]]), BC.dark, 24);
    ink(ctx, circle(560, 478, 54), BC.bone, 22);
    ink(ctx, circle(542, 474, 11), INK, 0);
    ink(ctx, circle(578, 474, 11), INK, 0);
  },

  hellgate(ctx) {
    ink(ctx, rrect(236, 800, 552, 96, 24), BC.stoneLo, BO);
    ink(ctx, poly([[446, 176], [512, 28], [578, 176]]), BC.stoneHi, BO);
    bothSides(ctx, () => ink(ctx, poly([[214, 330], [96, 214], [272, 262]]), BC.stoneHi, BO));
    ink(ctx, circle(512, 484, 346), BC.stone, BO);
    ink(ctx, circle(512, 484, 248), BC.ember, 26);
    ink(ctx, circle(512, 484, 204), BC.portal, 0);
    ctx.beginPath();
    for (let i = 0; i <= 60; i++) {
      const a = i * 0.21, r = 14 + i * 2.6;
      const x = 512 + Math.cos(a) * r, y = 484 + Math.sin(a) * r;
      i ? ctx.lineTo(x, y) : ctx.moveTo(x, y);
    }
    ctx.lineCap = 'round';
    ctx.lineWidth = 34;
    ctx.strokeStyle = BC.portalHi;
    ctx.stroke();
  },

  // ---------- card art for the two cards that are not buildings ----------
  provisions(ctx) {
    ink(ctx, poly([[430, 380], [372, 214], [652, 214], [594, 380]]), BC.sack, BO);
    ink(ctx, shape(c => {
      c.moveTo(430, 372);
      c.bezierCurveTo(120, 470, 150, 900, 512, 900);
      c.bezierCurveTo(874, 900, 904, 470, 594, 372);
      c.closePath();
    }), BC.sack, BO);
    line(ctx, [[280, 640], [330, 800]], BC.sackLo, 26);
    post(ctx, [[420, 380], [604, 380]], BC.red, 22);
    ink(ctx, rrect(452, 560, 168, 150, 22), BC.woodHi, 24);
    line(ctx, [[490, 600], [582, 670]], BC.woodLo, 14);
    line(ctx, [[582, 600], [490, 670]], BC.woodLo, 14);
  },
  pact(ctx) {
    ctx.save();
    ctx.scale(4, 4);
    ICONS.pact(ctx);
    ctx.restore();
  },
};

function renderBuilding(name) {
  const c = newCanvas(BS);
  BUILDINGS[name](c.getContext('2d'));
  return c;
}

// Each building large, then on a tile at desktop (61px) and phone (22px) size,
// scaled to the 0.55 of a cell the game gives a marker.
function buildingSheet(items, tiles) {
  const big = 180, pad = 12, rowH = big + 61 * 3 + 22 * 6 + pad * 4;
  const sheet = newCanvas(items.length * (big + pad) + pad, rowH + pad);
  const ctx = sheet.getContext('2d');
  ctx.fillStyle = '#140C0A';
  ctx.fillRect(0, 0, sheet.width, sheet.height);
  items.forEach((item, i) => {
    const x = pad + i * (big + pad);
    ctx.drawImage(mipDown(item.canvas, big), x, pad);
    [[61, 3, big + pad * 2], [22, 6, big + 61 * 3 + pad * 3]].forEach(([cell, k, y]) => {
      const t = newCanvas(cell);
      const tc = t.getContext('2d');
      tc.drawImage(mipDown(tiles[i % tiles.length], cell), 0, 0);
      const s = Math.round(cell * 0.55);
      tc.drawImage(mipDown(item.canvas, s), (cell - s) / 2, (cell - s) / 2 - cell * 0.15);
      ctx.drawImage(upscaleNearest(t, k), x + (big - cell * k) / 2, y);
    });
  });
  return sheet;
}

// ---------- card frame (authored at 4x of the 130 x 190 card) ----------
const CARD = { w: 520, h: 760 };
function drawCardFrame(ctx) {
  const { w, h } = CARD;
  ink(ctx, rrect(10, 10, w - 20, h - 20, 46), PC.frame, 20);
  ctx.save();
  ctx.beginPath(); rrect(20, 20, w - 40, h - 40, 38)(ctx); ctx.clip();
  ctx.lineWidth = 14;
  ctx.strokeStyle = PC.frameHi;
  ctx.beginPath(); rrect(26, 26, w - 52, h - 52, 34)(ctx); ctx.stroke();
  ctx.restore();

  const p = 52;
  ink(ctx, rrect(p, p, w - 2 * p, h - 2 * p, 26), '#F2DFAE', 14);
  ctx.save();
  ctx.beginPath(); rrect(p + 7, p + 7, w - 2 * p - 14, h - 2 * p - 14, 20)(ctx); ctx.clip();
  const rng = rngFrom(4242);
  for (let i = 0; i < 26; i++) {
    const r = rng.range(30, 80);
    fillPts(ctx, blobPts(rng.range(p, w - p), rng.range(p, h - p), r, r * rng.range(0.6, 1), rng, { n: 8, wob: 0.25 }),
      rng() < 0.5 ? 'rgba(255,244,205,0.30)' : 'rgba(196,150,84,0.14)');
  }
  ctx.lineWidth = 44;
  ctx.strokeStyle = 'rgba(170,112,48,0.26)';
  ctx.beginPath(); rrect(p, p, w - 2 * p, h - 2 * p, 26)(ctx); ctx.stroke();
  ctx.restore();

  [[34, 34], [w - 34, 34], [34, h - 34], [w - 34, h - 34]].forEach(([x, y]) => {
    ink(ctx, circle(x, y, 12), PC.rivet, 7);
    ink(ctx, circle(x - 3, y - 4, 4), PC.rivetHi, 0);
  });
  [36, h - 36].forEach(y => ink(ctx, poly([[w / 2, y - 30], [w / 2 + 30, y], [w / 2, y + 30], [w / 2 - 30, y]]), '#FF8A2A', 12));
}

// ---------- background ----------
// Tileable: every blob is drawn nine times, offset by the canvas size, so what
// leaves one edge comes back in on the other.
function renderBackdrop() {
  const n = 1024, c = newCanvas(n), ctx = c.getContext('2d');
  ctx.fillStyle = '#170D0B';
  ctx.fillRect(0, 0, n, n);
  const rng = rngFrom(9090);
  const wrapped = draw => {
    for (let dx = -n; dx <= n; dx += n) for (let dy = -n; dy <= n; dy += n) {
      ctx.save(); ctx.translate(dx, dy); draw(); ctx.restore();
    }
  };
  for (let i = 0; i < 46; i++) {
    const r = rng.range(60, 190), x = rng.range(0, n), y = rng.range(0, n);
    const pts = blobPts(x, y, r, r * rng.range(0.55, 1), rng, { n: 9, wob: 0.24 });
    const col = rng.pick(['#1D110E', '#120908', '#21140F', '#1A0F0C']);
    wrapped(() => fillPts(ctx, pts, col));
  }
  for (let i = 0; i < 16; i++) {                       // cracks
    const a = [rng.range(0, n), rng.range(0, n)], ang = rng.range(0, Math.PI * 2), len = rng.range(70, 190);
    const pts = jagged(rng, a, [a[0] + Math.cos(ang) * len, a[1] + Math.sin(ang) * len], 4, 22);
    wrapped(() => line(ctx, pts, '#0C0605', 9));
  }
  for (let i = 0; i < 22; i++) {                       // embers
    const x = rng.range(0, n), y = rng.range(0, n), r = rng.range(4, 9);
    wrapped(() => fillEllipse(ctx, x, y, r, r, rng() < 0.4 ? '#7A3516' : '#4A2212'));
  }
  return c;
}

function renderGlow() {
  const n = 512, c = newCanvas(n), ctx = c.getContext('2d');
  const g = ctx.createRadialGradient(n / 2, n / 2, 0, n / 2, n / 2, n / 2);
  g.addColorStop(0, 'rgba(232,110,42,0.55)');
  g.addColorStop(0.45, 'rgba(190,70,30,0.26)');
  g.addColorStop(1, 'rgba(120,30,20,0)');
  ctx.fillStyle = g;
  ctx.fillRect(0, 0, n, n);
  return c;
}
