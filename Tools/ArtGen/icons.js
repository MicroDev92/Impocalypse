'use strict';
// UI icons. Each is drawn on a 256 canvas with a transparent background and is
// shown at roughly 26-32px in the sidebars, so one rendered pixel is ~9 source
// pixels: every icon is one bold silhouette, a 14px ink outline and at most two
// interior details. Anything finer disappears.

const IS = 256;
const IC = {
  gold: '#F2B53C', goldHi: '#FFE08A', goldLo: '#B9781F',
  bone: '#F3E3C3', boneLo: '#CDB58E',
  ember: '#FF8A2A', emberHi: '#FFD36B',
  wood: '#6B4330', woodLo: '#4A2C20', woodEnd: '#D9A066',
  root: '#E0443A', rootLo: '#A82C2C', leaf: '#6DBB4E', leafLo: '#3F8436',
  soul: '#7FE8D2', soulHi: '#D9FFF5', soulLo: '#3FB79F',
  silk: '#A45BD6', silkHi: '#D2A2F2', silkLo: '#6E3A9C',
  wine: '#C2356E', wineHi: '#F07AA6',
  ecto: '#9BE35A', ectoHi: '#DEFFAE', ectoLo: '#5FA833',
  stone: '#8A7F92', stoneLo: '#5B5263', stoneHi: '#B4A9BC',
  tile: '#C96A35', tileHi: '#E18B50', tileLo: '#8E4322',
  paper: '#F0D49A', paperLo: '#C9A566', wax: '#C8322F',
  chest: '#8A5230', chestLo: '#5E3520',
};
const OUT = 14;

// Fill a path, then outline it in ink. `build` only builds the path.
function ink(ctx, build, fill, width = OUT) {
  ctx.beginPath();
  build(ctx);
  ctx.lineJoin = 'round';
  ctx.lineCap = 'round';
  if (fill) { ctx.fillStyle = fill; ctx.fill(); }
  if (width > 0) { ctx.lineWidth = width; ctx.strokeStyle = INK; ctx.stroke(); }
}
const poly = pts => ctx => { pts.forEach((p, i) => (i ? ctx.lineTo(p[0], p[1]) : ctx.moveTo(p[0], p[1]))); ctx.closePath(); };
const circle = (x, y, r) => ctx => ctx.arc(x, y, r, 0, Math.PI * 2);
const ellipse = (x, y, rx, ry, rot = 0) => ctx => ctx.ellipse(x, y, rx, ry, rot, 0, Math.PI * 2);
const rrect = (x, y, w, h, r) => ctx => {
  ctx.moveTo(x + r, y);
  ctx.arcTo(x + w, y, x + w, y + h, r);
  ctx.arcTo(x + w, y + h, x, y + h, r);
  ctx.arcTo(x, y + h, x, y, r);
  ctx.arcTo(x, y, x + w, y, r);
  ctx.closePath();
};
function line(ctx, pts, color, width) {
  ctx.beginPath();
  pts.forEach((p, i) => (i ? ctx.lineTo(p[0], p[1]) : ctx.moveTo(p[0], p[1])));
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';
  ctx.lineWidth = width;
  ctx.strokeStyle = color;
  ctx.stroke();
}

const ICONS = {
  // ---------- resources ----------
  charwood(ctx) {                       // a charred log with a glowing crack
    ctx.save();
    ctx.translate(128, 132);
    ctx.rotate(-0.42);
    ink(ctx, rrect(-104, -44, 190, 88, 40), IC.wood);
    line(ctx, [[-70, 20], [-20, 24], [30, 18]], IC.woodLo, 12);
    line(ctx, [[-66, -8], [-34, 6], [-10, -12], [22, 2]], IC.ember, 13);
    line(ctx, [[-60, -9], [-34, 4], [-12, -12]], IC.emberHi, 5);
    ink(ctx, ellipse(84, 0, 30, 44), IC.woodEnd);
    ink(ctx, ellipse(84, 0, 11, 18), null, 8);
    ctx.restore();
  },
  bloodroot(ctx) {                      // a fat red root with two leaves
    ink(ctx, poly([[128, 86], [86, 30], [60, 62], [100, 98]]), IC.leafLo);
    ink(ctx, poly([[132, 88], [150, 22], [196, 46], [164, 100]]), IC.leaf);
    ink(ctx, c => {
      c.moveTo(72, 104);
      c.bezierCurveTo(70, 70, 186, 70, 186, 106);
      c.bezierCurveTo(186, 160, 150, 214, 126, 238);
      c.bezierCurveTo(100, 212, 70, 160, 72, 104);
      c.closePath();
    }, IC.root);
    line(ctx, [[98, 128], [126, 136]], IC.rootLo, 11);
    line(ctx, [[128, 168], [150, 162]], IC.rootLo, 11);
  },
  souls(ctx) {                          // a little ghost
    ink(ctx, c => {
      c.moveTo(52, 216);
      c.bezierCurveTo(40, 20, 216, 20, 204, 216);
      c.lineTo(172, 188);
      c.lineTo(146, 220);
      c.lineTo(118, 188);
      c.lineTo(88, 220);
      c.closePath();
    }, IC.soul);
    ink(ctx, ellipse(100, 112, 13, 20), INK, 0);
    ink(ctx, ellipse(156, 112, 13, 20), INK, 0);
    line(ctx, [[76, 84], [92, 60]], IC.soulHi, 10);
  },
  demonsilk(ctx) {                      // a bolt of purple cloth
    ink(ctx, rrect(40, 70, 176, 128, 30), IC.silk);
    line(ctx, [[64, 108], [192, 108]], IC.silkHi, 13);
    line(ctx, [[64, 162], [192, 162]], IC.silkLo, 13);
    ink(ctx, poly([[150, 160], [226, 150], [214, 232], [162, 222]]), IC.silkHi);
  },
  soulwine(ctx) {                       // a goblet
    ink(ctx, rrect(84, 206, 88, 26, 10), IC.gold);
    ink(ctx, rrect(116, 150, 24, 62, 6), IC.gold);
    ink(ctx, c => {
      c.moveTo(54, 36);
      c.lineTo(202, 36);
      c.bezierCurveTo(206, 130, 172, 162, 128, 162);
      c.bezierCurveTo(84, 162, 50, 130, 54, 36);
      c.closePath();
    }, IC.gold);
    ink(ctx, ellipse(128, 44, 62, 20), IC.wine, 10);
    line(ctx, [[96, 40], [126, 36]], IC.wineHi, 8);
  },
  ectoplasm(ctx) {                      // a slime blob with a drip
    ink(ctx, c => {
      c.moveTo(40, 150);
      c.bezierCurveTo(30, 40, 226, 40, 216, 150);
      c.bezierCurveTo(214, 186, 188, 190, 180, 170);
      c.bezierCurveTo(176, 226, 138, 232, 136, 180);
      c.bezierCurveTo(124, 204, 96, 204, 92, 176);
      c.bezierCurveTo(74, 196, 42, 188, 40, 150);
      c.closePath();
    }, IC.ecto);
    ink(ctx, circle(102, 106, 20), IC.ectoHi, 0);
    ink(ctx, circle(160, 128, 11), IC.ectoHi, 0);
    line(ctx, [[176, 78], [190, 96]], IC.ectoLo, 10);
  },
  any(ctx) {                            // wildcard: one disc, three resources
    const cols = [IC.wood, IC.root, IC.soul];
    cols.forEach((col, i) => {
      const a0 = -Math.PI / 2 + (i * Math.PI * 2) / 3;
      ink(ctx, c => { c.moveTo(128, 128); c.arc(128, 128, 96, a0, a0 + (Math.PI * 2) / 3); c.closePath(); }, col, 10);
    });
    ink(ctx, circle(128, 128, 96), null);
    ink(ctx, circle(128, 128, 22), IC.bone, 10);
  },

  // ---------- scoring ----------
  strength(ctx) {                       // a citadel tower with a lit doorway
    ink(ctx, poly([[66, 232], [78, 104], [178, 104], [190, 232]]), IC.stone);
    ink(ctx, poly([[52, 112], [52, 36], [88, 36], [88, 62], [110, 62], [110, 36], [146, 36], [146, 62], [168, 62], [168, 36], [204, 36], [204, 112]]), IC.stoneHi);
    ink(ctx, c => {
      c.moveTo(102, 232);
      c.lineTo(102, 176);
      c.bezierCurveTo(102, 136, 154, 136, 154, 176);
      c.lineTo(154, 232);
      c.closePath();
    }, IC.ember, 10);
    line(ctx, [[84, 150], [84, 200]], IC.stoneLo, 10);
  },
  wealth(ctx) {                         // three stacked coins
    [[128, 190], [128, 142], [128, 94]].forEach(([x, y], i) => {
      ink(ctx, c => {
        c.moveTo(x - 78, y);
        c.lineTo(x - 78, y + 26);
        c.bezierCurveTo(x - 78, y + 62, x + 78, y + 62, x + 78, y + 26);
        c.lineTo(x + 78, y);
        c.closePath();
      }, IC.goldLo);
      ink(ctx, ellipse(x, y, 78, 34), i === 2 ? IC.goldHi : IC.gold);
    });
    ink(ctx, ellipse(128, 94, 38, 14), IC.gold, 8);
  },
  infamy(ctx) {                         // a horned skull
    ink(ctx, poly([[66, 96], [24, 30], [92, 62]]), IC.ember);
    ink(ctx, poly([[190, 96], [232, 30], [164, 62]]), IC.ember);
    ink(ctx, c => {
      c.moveTo(48, 134);
      c.bezierCurveTo(44, 30, 212, 30, 208, 134);
      c.bezierCurveTo(208, 166, 190, 178, 178, 184);
      c.lineTo(178, 226);
      c.lineTo(78, 226);
      c.lineTo(78, 184);
      c.bezierCurveTo(66, 178, 48, 166, 48, 134);
      c.closePath();
    }, IC.bone);
    ink(ctx, ellipse(96, 128, 21, 25), INK, 0);
    ink(ctx, ellipse(160, 128, 21, 25), INK, 0);
    ink(ctx, poly([[128, 152], [116, 176], [140, 176]]), INK, 0);
    line(ctx, [[110, 198], [110, 226]], INK, 9);
    line(ctx, [[146, 198], [146, 226]], INK, 9);
  },

  // ---------- board ----------
  tile(ctx) {                           // one territory
    ink(ctx, rrect(36, 36, 184, 184, 36), IC.tile);
    line(ctx, [[72, 84], [118, 120], [104, 170]], IC.tileLo, 13);
    line(ctx, [[118, 120], [176, 108]], IC.tileLo, 13);
  },
  domain(ctx) {                         // three joined territories
    ink(ctx, rrect(22, 22, 104, 104, 22), IC.tileHi);
    ink(ctx, rrect(22, 130, 104, 104, 22), IC.tile);
    ink(ctx, rrect(130, 130, 104, 104, 22), IC.tile);
    line(ctx, [[74, 112], [74, 144]], IC.goldHi, 16);
    line(ctx, [[112, 182], [144, 182]], IC.goldHi, 16);
  },

  // ---------- pacts ----------
  pact(ctx) {                           // a sealed parchment
    ink(ctx, rrect(52, 34, 152, 188, 16), IC.paper);
    line(ctx, [[82, 78], [174, 78]], IC.paperLo, 12);
    line(ctx, [[82, 112], [174, 112]], IC.paperLo, 12);
    line(ctx, [[82, 146], [136, 146]], IC.paperLo, 12);
    ink(ctx, circle(166, 184, 34), IC.wax);
  },
  treasure(ctx) {                       // a chest
    ink(ctx, rrect(34, 110, 188, 116, 14), IC.chest);
    ink(ctx, c => {
      c.moveTo(34, 118);
      c.bezierCurveTo(34, 22, 222, 22, 222, 118);
      c.closePath();
    }, IC.chestLo);
    line(ctx, [[34, 118], [222, 118]], IC.gold, 16);
    line(ctx, [[34, 118], [222, 118]], INK, 4);
    ink(ctx, rrect(106, 98, 44, 52, 10), IC.gold);
    ink(ctx, circle(128, 122, 8), INK, 0);
  },
};

function renderIcon(name) {
  const c = newCanvas(IS);
  ICONS[name](c.getContext('2d'));
  return c;
}
