'use strict';
// Renders the tile set, shows it, and (with ?save) uploads the PNGs plus the
// review sheets to ./out via server.py.
//
// The review sheets matter more than the full-size tiles: a tile is judged at
// the size the game shows it, so everything is also rendered at 61px (desktop)
// and 22px (phone) on a mock board with the same rotation/mirroring the game
// applies.

function renderTile(def) {
  const canvas = newCanvas(S);
  tileFrame(canvas.getContext('2d'), ctx => def.paint(ctx));
  return canvas;
}

// Clustered 10x10 layout with the game's terrain quotas (40/16/16/16/12).
function makeLayout(rng) {
  const quota = { P: 40, W: 16, G: 16, F: 16, M: 12 };
  const keys = Object.keys(quota);
  const grid = [];
  for (let y = 0; y < 10; y++) {
    grid.push([]);
    for (let x = 0; x < 10; x++) {
      const w = keys.map(k => {
        let v = quota[k];
        if (x > 0 && grid[y][x - 1] === k) v *= 3;
        if (y > 0 && grid[y - 1][x] === k) v *= 3;
        return v;
      });
      let roll = rng() * w.reduce((a, b) => a + b, 0), pick = null;
      for (let i = 0; i < keys.length; i++) {
        roll -= w[i];
        if (roll <= 0 && w[i] > 0) { pick = keys[i]; break; }
      }
      if (!pick) pick = keys.find(k => quota[k] > 0);
      quota[pick]--;
      grid[y].push(pick);
    }
  }
  return grid;
}

function renderBoard(tiles, cell, seed) {
  const rng = rngFrom(seed);
  const layout = makeLayout(rng);
  const board = newCanvas(cell * 10);
  const ctx = board.getContext('2d');
  const mips = new Map();
  const mip = t => {
    if (!mips.has(t)) mips.set(t, mipDown(t.canvas, cell));
    return mips.get(t);
  };
  for (let y = 0; y < 10; y++) {
    for (let x = 0; x < 10; x++) {
      const type = layout[y][x];
      const t = rng.pick(tiles.filter(q => q.type === type));
      let rot = 0, flip = false;
      if (type === 'P') { rot = rng.int(0, 3); flip = rng() < 0.5; }
      else if (type === 'W') rot = rng.int(0, 3);
      else if (type === 'G' || type === 'M') flip = rng() < 0.5;
      ctx.save();
      ctx.translate(x * cell + cell / 2, y * cell + cell / 2);
      ctx.rotate(rot * Math.PI / 2);
      if (flip) ctx.scale(-1, 1);
      ctx.drawImage(mip(t), -cell / 2, -cell / 2);
      ctx.restore();
    }
  }
  return board;
}

function upscaleNearest(src, k) {
  const out = newCanvas(src.width * k, src.height * k);
  const ctx = out.getContext('2d');
  ctx.imageSmoothingEnabled = false;
  ctx.drawImage(src, 0, 0, out.width, out.height);
  return out;
}

function contactSheet(tiles, cell, cols, draw) {
  const rows = Math.ceil(tiles.length / cols), pad = 14, label = 26;
  const sheet = newCanvas(cols * (cell + pad) + pad, rows * (cell + pad + label) + pad);
  const ctx = sheet.getContext('2d');
  ctx.fillStyle = '#140C0A';
  ctx.fillRect(0, 0, sheet.width, sheet.height);
  ctx.font = '18px sans-serif';
  ctx.fillStyle = '#F0A868';
  tiles.forEach((t, i) => {
    const x = pad + (i % cols) * (cell + pad), y = pad + Math.floor(i / cols) * (cell + pad + label);
    draw(ctx, t, x, y);
    ctx.fillText(t.name, x + 2, y + cell + 19);
  });
  return sheet;
}

async function main() {
  const save = new URLSearchParams(location.search).has('save');
  const tiles = TILESET.map(def => ({ ...def, canvas: renderTile(def) }));

  const sheetFull = contactSheet(tiles, 256, 5, (ctx, t, x, y) => ctx.drawImage(mipDown(t.canvas, 256), x, y));
  const sheet61 = contactSheet(tiles, 244, 5, (ctx, t, x, y) => ctx.drawImage(upscaleNearest(mipDown(t.canvas, 61), 4), x, y));
  const board61 = renderBoard(tiles, 61, 77);
  const board22 = renderBoard(tiles, 22, 77);
  const board122 = renderBoard(tiles, 122, 77);

  const show = (c, title) => {
    const h = document.createElement('h3');
    h.textContent = title;
    document.body.append(h, c);
  };
  show(sheetFull, 'Tiles');
  show(board122, 'Mock board, 122px tiles (2x desktop)');
  show(board61, 'Mock board, 61px tiles (desktop)');
  show(board22, 'Mock board, 22px tiles (phone)');
  show(sheet61, 'Each tile at 61px, shown 4x');

  let saved = 0;
  if (save) {
    for (const t of tiles) { await saveCanvas(t.canvas, t.name + '.png'); saved++; }
    await saveCanvas(sheetFull, '_sheet_tiles.png');
    await saveCanvas(sheet61, '_sheet_61px_x4.png');
    await saveCanvas(board122, '_board_122.png');
    await saveCanvas(upscaleNearest(board61, 2), '_board_61_x2.png');
    await saveCanvas(upscaleNearest(board22, 4), '_board_22_x4.png');
    saved += 5;
  }
  document.title = 'DONE ' + saved;
}

main().catch(e => {
  document.title = 'ERROR ' + e.message;
  console.error(e);
});
