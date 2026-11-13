'use strict';
// The four player creatures. Anchoring is shared and comes from ASSET_BRIEF.md:
// 1024 canvas = one world unit, feet on a baseline 8% up from the bottom, art
// inside 8%..96% vertically and the middle 80% horizontally.
//
// A creature is ~48px tall on desktop and ~17px on a phone, so each one is a
// different SHAPE first (horned imp / wide blob in a nightcap / spiky X / caped
// triangle with a crown) and a different colour second.

const CS = 1024;
const CO = 30; // outline width
const CR = { bone: '#F3E3C3', gold: '#F2B53C', goldHi: '#FFE08A', mouth: '#5A1E1E' };

function tube(ctx, pts, color, w, out = 24) {
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';
  smoothOpen(ctx, pts);
  ctx.lineWidth = w + out * 2;
  ctx.strokeStyle = INK;
  ctx.stroke();
  smoothOpen(ctx, pts);
  ctx.lineWidth = w;
  ctx.strokeStyle = color;
  ctx.stroke();
}
function mirrored(ctx, fn) {
  fn();
  ctx.save();
  ctx.translate(CS, 0);
  ctx.scale(-1, 1);
  fn();
  ctx.restore();
}
// Eye with an optional heavy lid covering the top `lid` fraction.
function eye(ctx, x, y, rx, ry, px, py, pr, lidColor, lid = 0) {
  ink(ctx, ellipse(x, y, rx, ry), CR.bone, 0);
  ink(ctx, circle(px, py, pr), INK, 0);
  if (lidColor) {
    ctx.save();
    ctx.beginPath();
    ctx.ellipse(x, y, rx, ry, 0, 0, Math.PI * 2);
    ctx.clip();
    ctx.fillStyle = lidColor;
    ctx.fillRect(x - rx, y - ry, rx * 2, ry * 2 * lid);
    ctx.fillStyle = INK;
    ctx.fillRect(x - rx, y - ry + ry * 2 * lid - 8, rx * 2, 16);
    ctx.restore();
  }
  ink(ctx, ellipse(x, y, rx, ry), null, 18);
}
const path = build => ctx => build(ctx);

const CREATURES = {
  // ---------- 1: red troublemaker - the classic imp ----------
  red(ctx) {
    const m = '#E04A2F', d = '#B23322', l = '#F26B4C';
    tube(ctx, [[590, 800], [740, 870], [850, 800], [840, 680]], m, 30);
    ink(ctx, poly([[840, 560], [915, 690], [840, 660], [765, 700]]), m, CO);
    mirrored(ctx, () => ink(ctx, ellipse(424, 902, 84, 36), d, CO));
    ink(ctx, ellipse(512, 760, 140, 150), m, CO);
    ink(ctx, ellipse(512, 785, 78, 88), l, 0);
    mirrored(ctx, () => tube(ctx, [[400, 690], [300, 770], [390, 835]], m, 38));
    mirrored(ctx, () => ink(ctx, poly([[290, 400], [112, 320], [250, 545]]), d, CO));
    mirrored(ctx, () => ink(ctx, path(c => {
      c.moveTo(285, 320);
      c.quadraticCurveTo(215, 150, 300, 48);
      c.quadraticCurveTo(335, 175, 425, 215);
      c.closePath();
    }), CR.bone, CO));
    ink(ctx, ellipse(512, 440, 262, 236), m, CO);
    mirrored(ctx, () => eye(ctx, 418, 415, 72, 82, 436, 428, 27));
    mirrored(ctx, () => line(ctx, [[338, 322], [474, 356]], INK, 28));
    ink(ctx, path(c => {
      c.moveTo(366, 528);
      c.quadraticCurveTo(512, 668, 658, 528);
      c.quadraticCurveTo(512, 566, 366, 528);
      c.closePath();
    }), CR.bone, 18);
    [462, 512, 562].forEach(x => line(ctx, [[x, 552], [x, 596]], INK, 10));
  },

  // ---------- 2: teal sleepyhead - a wide blob under a drooping nightcap ----------
  teal(ctx) {
    const m = '#5FC9B8', d = '#3E9E90', l = '#BFF1E6', cap = '#2F5F8F';
    mirrored(ctx, () => ink(ctx, path(c => {
      c.moveTo(250, 520);
      c.quadraticCurveTo(84, 560, 112, 750);
      c.quadraticCurveTo(200, 690, 300, 630);
      c.closePath();
    }), d, CO));
    ink(ctx, path(c => {
      c.moveTo(200, 896);
      c.bezierCurveTo(150, 560, 300, 330, 512, 330);
      c.bezierCurveTo(724, 330, 874, 560, 824, 896);
      c.quadraticCurveTo(512, 962, 200, 896);
      c.closePath();
    }), m, CO);
    ink(ctx, ellipse(512, 770, 175, 125), l, 0);
    mirrored(ctx, () => ink(ctx, ellipse(372, 908, 84, 32), d, CO));
    ink(ctx, poly([[300, 440], [236, 318], [360, 380]]), CR.bone, CO);
    // nightcap: over the crown, tip flopped down the right side
    ink(ctx, path(c => {
      c.moveTo(322, 410);
      c.quadraticCurveTo(380, 150, 610, 132);
      c.quadraticCurveTo(820, 130, 880, 380);
      c.quadraticCurveTo(770, 270, 706, 410);
      c.closePath();
    }), cap, CO);
    tube(ctx, [[318, 424], [512, 368], [710, 424]], CR.bone, 46);
    ink(ctx, circle(884, 410, 52), CR.bone, CO);
    mirrored(ctx, () => eye(ctx, 408, 566, 64, 52, 408, 592, 18, d, 0.55));
    ink(ctx, ellipse(512, 696, 34, 44), CR.mouth, 16);
  },

  // ---------- 3: yellow maniac - all spikes, arms in the air ----------
  yellow(ctx) {
    const m = '#F2B53C', d = '#C98A1E', flame = '#FF8A2A';
    mirrored(ctx, () => tube(ctx, [[462, 780], [392, 890]], m, 40));
    mirrored(ctx, () => ink(ctx, ellipse(360, 910, 74, 30), d, CO));
    mirrored(ctx, () => tube(ctx, [[430, 650], [262, 610], [178, 468]], m, 38));
    mirrored(ctx, () => ink(ctx, poly([[122, 480], [112, 372], [158, 410], [176, 318], [208, 408], [262, 372], [236, 486]]), m, CO));
    ink(ctx, ellipse(512, 690, 112, 150), m, CO);
    [[[326, 262], [286, 64], [432, 222]], [[446, 232], [512, 42], [578, 232]], [[592, 222], [738, 64], [698, 262]]]
      .forEach(p => ink(ctx, poly(p), flame, CO));
    ink(ctx, path(c => {
      c.moveTo(276, 256);
      c.quadraticCurveTo(512, 196, 748, 256);
      c.quadraticCurveTo(800, 430, 644, 548);
      c.quadraticCurveTo(512, 596, 380, 548);
      c.quadraticCurveTo(224, 430, 276, 256);
      c.closePath();
    }), m, CO);
    eye(ctx, 418, 362, 80, 80, 418, 362, 16);
    eye(ctx, 612, 382, 54, 54, 612, 382, 12);
    ink(ctx, poly([[378, 474], [430, 504], [470, 468], [512, 508], [554, 468], [594, 504], [646, 474], [600, 544], [512, 564], [424, 544]]), CR.bone, 16);
  },

  // ---------- 4: purple wannabe royalty - cape, crown, sceptre ----------
  purple(ctx) {
    const m = '#8B6CC9', d = '#6248A0', cape = '#3F2A6E';
    ink(ctx, poly([[404, 520], [196, 916], [828, 916], [620, 520]]), cape, CO);
    line(ctx, [[236, 884], [788, 884]], CR.gold, 18);
    mirrored(ctx, () => ink(ctx, ellipse(446, 912, 66, 28), d, CO));
    ink(ctx, ellipse(512, 742, 112, 160), m, CO);
    tube(ctx, [[426, 660], [338, 724], [412, 786]], m, 36);
    line(ctx, [[722, 870], [722, 500]], INK, 46);
    line(ctx, [[722, 866], [722, 504]], CR.gold, 16);
    ink(ctx, circle(722, 462, 46), CR.goldHi, CO);
    tube(ctx, [[598, 660], [700, 700]], m, 36);
    ink(ctx, rrect(378, 536, 268, 76, 38), CR.bone, CO);
    [440, 512, 584].forEach(x => ink(ctx, circle(x, 574, 9), INK, 0));
    mirrored(ctx, () => ink(ctx, poly([[352, 340], [246, 276], [346, 436]]), d, CO));
    mirrored(ctx, () => ink(ctx, poly([[376, 236], [322, 108], [440, 190]]), CR.bone, CO));
    ink(ctx, ellipse(512, 362, 172, 200), m, CO);
    ink(ctx, poly([[392, 214], [370, 62], [450, 134], [512, 42], [574, 134], [654, 62], [632, 214]]), CR.gold, CO);
    ink(ctx, circle(512, 168, 18), '#C8322F', 10);
    eye(ctx, 444, 356, 50, 42, 460, 372, 14, d, 0.5);
    eye(ctx, 580, 356, 50, 42, 596, 372, 14, d, 0.5);
    line(ctx, [[398, 286], [482, 272]], INK, 18);
    line(ctx, [[542, 272], [626, 286]], INK, 18);
    ctx.lineCap = 'round';
    smoothOpen(ctx, [[458, 464], [520, 486], [584, 444]]);
    ctx.lineWidth = 18;
    ctx.strokeStyle = INK;
    ctx.stroke();
  },
};

function renderCreature(name) {
  const c = newCanvas(CS);
  CREATURES[name](c.getContext('2d'));
  return c;
}

function loadImage(src) {
  return new Promise(res => {
    const img = new Image();
    img.onload = () => {
      const c = newCanvas(img.width, img.height);
      c.getContext('2d').drawImage(img, 0, 0);
      res(c);
    };
    img.onerror = () => res(null);
    img.src = src;
  });
}

// Rows of creature sets, each shown large, then standing on real tiles at the
// desktop (61px) and phone (22px) tile sizes with the game's 0.9 spawn scale.
function creatureSheet(sets, tiles) {
  const big = 200, pad = 12;
  const cols = 4, rowH = big + 61 * 3 + 22 * 6 + pad * 5;
  const sheet = newCanvas(cols * (big + pad) + pad, sets.length * rowH + pad);
  const ctx = sheet.getContext('2d');
  ctx.fillStyle = '#140C0A';
  ctx.fillRect(0, 0, sheet.width, sheet.height);
  const onTile = (creature, tile, cell, k, x, y) => {
    const t = newCanvas(cell);
    const tc = t.getContext('2d');
    tc.drawImage(mipDown(tile, cell), 0, 0);
    const s = Math.round(cell * 0.9);
    tc.drawImage(mipDown(creature, s), (cell - s) / 2, (cell - s) / 2);
    ctx.drawImage(upscaleNearest(t, k), x, y);
  };
  sets.forEach((set, r) => {
    set.forEach((creature, i) => {
      if (!creature) return;
      const x = pad + i * (big + pad), y = pad + r * rowH;
      ctx.drawImage(mipDown(creature, big), x, y);
      onTile(creature, tiles[i % tiles.length], 61, 3, x + 8, y + big + pad);
      onTile(creature, tiles[i % tiles.length], 22, 6, x + 34, y + big + 61 * 3 + pad * 2);
    });
  });
  return sheet;
}
