'use strict';
// UI chrome: sidebar panel, recessed inner panel, button and top bar.
//
// All of it is 9-sliced, so each painter takes a size and keeps every bit of
// decoration inside the corner it belongs to; the edges between corners are
// flat bands that survive stretching. Authored at 2x (import at PPU 200), so a
// 10px line here is a 5px line on a 1080p canvas.

const PC = {
  frame: '#6B4330', frameHi: '#8A5A3F', frameLo: '#4A2C20',
  fill: '#2A1915', fillLo: '#1E120E',
  rivet: '#F2B53C', rivetHi: '#FFE08A',
  btn: '#C8472A', btnHi: '#E8703F', btnLo: '#8A2A1C',
};

// Sidebar / dialog frame: ink outline, a wooden band, an ink line, a dark fill.
function drawPanel(ctx, w, h) {
  const band = 22;
  ink(ctx, rrect(5, 5, w - 10, h - 10, 40), PC.frame, 10);
  // light top-left / dark bottom-right on the band, kept symmetric per corner
  ctx.save();
  ctx.beginPath(); rrect(10, 10, w - 20, h - 20, 35)(ctx);
  ctx.clip();
  ctx.lineWidth = 8;
  ctx.strokeStyle = PC.frameHi;
  ctx.beginPath(); rrect(14, 14, w - 28, h - 28, 32)(ctx); ctx.stroke();
  ctx.restore();
  const i = 5 + 5 + band;
  ink(ctx, rrect(i, i, w - 2 * i, h - 2 * i, 20), PC.fill, 7);
  // rivets, one per corner, sitting on the band
  [[26, 26], [w - 26, 26], [26, h - 26], [w - 26, h - 26]].forEach(([x, y]) => {
    ink(ctx, circle(x, y, 9), PC.rivet, 5);
    ink(ctx, circle(x - 2, y - 3, 3), PC.rivetHi, 0);
  });
}
const PANEL = { size: 256, border: 80 };

// Recess inside a panel: lists, the tile preview, the build-phase block.
function drawInset(ctx, w, h) {
  ink(ctx, rrect(4, 4, w - 8, h - 8, 22), PC.fillLo, 6);
  ctx.save();
  ctx.beginPath(); rrect(7, 7, w - 14, h - 14, 19)(ctx); ctx.clip();
  ctx.lineWidth = 10;
  ctx.strokeStyle = 'rgba(0,0,0,0.35)';
  ctx.beginPath(); rrect(7, 7, w - 14, h - 14, 19)(ctx); ctx.stroke();
  ctx.restore();
}
const INSET = { size: 128, border: 40 };

// Button: ember red slab with a darker lip underneath and a light top edge.
function drawButton(ctx, w, h) {
  ink(ctx, rrect(5, 5, w - 10, h - 10, 30), PC.btnLo, 10);
  ink(ctx, rrect(10, 10, w - 20, h - 34, 25), PC.btn, 0);
  ctx.save();
  ctx.beginPath(); rrect(10, 10, w - 20, h - 34, 25)(ctx); ctx.clip();
  ctx.lineWidth = 12;
  ctx.strokeStyle = PC.btnHi;
  ctx.beginPath(); ctx.moveTo(30, 16); ctx.lineTo(w - 30, 16); ctx.lineCap = 'round'; ctx.stroke();
  ctx.restore();
}
const BUTTON = { w: 256, h: 128, border: 48 };

// Top bar: full-width strip, so only its bottom edge is decorated.
function drawTopBar(ctx, w, h) {
  ctx.fillStyle = PC.fill;
  ctx.fillRect(0, 0, w, h);
  ctx.fillStyle = INK;
  ctx.fillRect(0, h - 26, w, 26);
  ctx.fillStyle = PC.frame;
  ctx.fillRect(0, h - 20, w, 14);
  ctx.fillStyle = PC.frameHi;
  ctx.fillRect(0, h - 20, w, 4);
}
const TOPBAR = { w: 64, h: 100, border: 30 };

function renderChrome() {
  const make = (w, h, draw) => { const c = newCanvas(w, h); draw(c.getContext('2d'), w, h); return c; };
  return [
    { name: 'panel_bg', canvas: make(PANEL.size, PANEL.size, drawPanel) },
    { name: 'panel_inset', canvas: make(INSET.size, INSET.size, drawInset) },
    { name: 'button_bg', canvas: make(BUTTON.w, BUTTON.h, drawButton) },
    { name: 'topbar_bg', canvas: make(TOPBAR.w, TOPBAR.h, drawTopBar) },
  ];
}

// A sidebar at real proportions (2x), to judge the frame at the sizes it is used.
function chromePreview() {
  const c = newCanvas(1000, 900);
  const ctx = c.getContext('2d');
  ctx.fillStyle = '#140C0A';
  ctx.fillRect(0, 0, c.width, c.height);
  const at = (x, y, w, h, draw) => { ctx.save(); ctx.translate(x, y); draw(ctx, w, h); ctx.restore(); };
  at(0, 0, 1000, 100, drawTopBar);
  at(20, 120, 760, 760, drawPanel);
  at(72, 180, 656, 300, drawInset);
  at(72, 500, 656, 200, drawInset);
  at(180, 730, 440, 128, drawButton);
  at(800, 140, 180, 104, drawButton);
  ctx.font = '52px "Luckiest Guy", Impact, sans-serif';
  ctx.fillStyle = '#F5E8D4';
  ctx.textAlign = 'center';
  ctx.fillText('DONE', 400, 806);
  return c;
}
