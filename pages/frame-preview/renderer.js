// Frame Previewer renderer — draws a Slay the Spire 2 card the way NCard does, on a <canvas>.
//
// Layout numbers are the node rects of gamedata/scenes/cards/card.tscn (card units, origin = card
// centre); colours come from the game's hsv.gdshader, ported below, fed the frame material of the
// style and the banner material of the rarity (tools/build-frame-preview.py exports both).
// Verified 2026-10-31 against a MegaDot 4.5 render of card.tscn itself: art layers + colours match to
// the pixel, text within ~1px (Godot draws MSDF glyphs; see the metric notes below).
(function () {
  'use strict';
  const A = window.FRAME_ASSETS;

  // ---- layout (card.tscn) -------------------------------------------------------------------
  const L = {
    bounds: { x: -170, y: -230, w: 340, h: 456 },
    shadow: [-138, -199, 300, 422],     // frame silhouette, modulate (0,0,0,0.25)
    frame: [-150, -211, 300, 422],      // KEEP_ASPECT_CENTERED
    portrait: [-125, -168, 250, 190],
    border: [-137.5, -164, 275, 210],   // KEEP_ASPECT_CENTERED, banner material
    banner: [-163, -207, 327, 83],      // KEEP_ASPECT_COVERED, banner material
    title: [-105, -204, 210, 54],
    plaque: { y: 1, h: 37, minW: 61, pad: 17, labelMinW: 44, marginL: 13, marginR: 12 },
    energy: [-166, -227, 64, 64],
    energyLabel: [-157, -221, 46, 56],  // EnergyLabel rect inside the icon
    desc: [-122, 37, 243, 136],
    // Ancient (full-art) frame — the Ancient* nodes
    ancientPortrait: [-153, -215, 299, 421],                    // 598x842 @ 0.5, STRETCH_SCALE, canvas-group mask
    ancientGlass: [-148.46497, -210.71002, 295.2725, 416.005],  // 590.5x832 @ 0.5
    ancientBorder: [-154, -223, 306, 440],                      // KEEP_ASPECT_CENTERED, additive
    ancientTextBg: [-133, -22, 264, 203],                       // KEEP_ASPECT_CENTERED, modulate (0,0,0,.66)
    ancientFlame: { x: 1, y: -217, scale: 0.6 },                // AncientBanner/Fire, centred sprite
  };
  const COLORS = {
    cream: '#FFF6E2', green: '#7FFF00', titleUpgradedOutline: '#1B6131', energyOutline: '#5C5440',
    gold: '#EFC851', red: '#FF5555', blue: '#87CEEB', purple: '#EE82EE', orange: '#FFA518', pink: '#FF78A0', aqua: '#2AEBBE',
  };
  const FRAME_OF = { attack: 'attack', skill: 'skill', power: 'power', status: 'skill', curse: 'skill' };

  // ---- assets -------------------------------------------------------------------------------
  const imgs = {};
  let readyPromise = null;
  function loadImage(src) {
    return new Promise((res, rej) => { const i = new Image(); i.onload = () => res(i); i.onerror = rej; i.src = src; });
  }
  function ready() {
    if (readyPromise) return readyPromise;
    const fontLoads = [['FP Kreon', A.fonts.kreon_regular, '400'], ['FP Kreon', A.fonts.kreon_bold, '700']].map(([fam, src, weight]) => {
      const f = new FontFace(fam, `url(${src})`, { weight });
      document.fonts.add(f);
      return f.load();
    });
    const imgLoads = Object.entries(A.images).map(([k, src]) => loadImage(src).then(i => { imgs[k] = i; }));
    readyPromise = Promise.all([...fontLoads, ...imgLoads]);
    return readyPromise;
  }

  // ---- hsv.gdshader port --------------------------------------------------------------------
  // GLSL: YIQ transform, hue rotation by (1-h)*2pi, scale I/Q by s, scale all by v, back to RGB.
  const YIQ = [[0.2989, 0.5870, 0.1140], [0.5959, -0.2774, -0.3216], [0.2115, -0.5229, 0.3114]];
  const INV = invert3(YIQ);
  function invert3(m) {
    const [[a, b, c], [d, e, f], [g, h, i]] = m;
    const A_ = e * i - f * h, B = -(d * i - f * g), C = d * h - e * g;
    const det = a * A_ + b * B + c * C;
    return [[A_ / det, -(b * i - c * h) / det, (b * f - c * e) / det],
            [B / det, (a * i - c * g) / det, -(a * f - c * d) / det],
            [C / det, -(a * h - b * g) / det, (a * e - b * d) / det]];
  }
  const hsvCache = new Map();
  function hsv(name, p) {
    const key = `${name}|${p.h}|${p.s}|${p.v}`;
    if (hsvCache.has(key)) return hsvCache.get(key);
    const src = imgs[name];
    const c = document.createElement('canvas');
    c.width = src.width; c.height = src.height;
    const x = c.getContext('2d', { willReadFrequently: true });
    x.drawImage(src, 0, 0);
    const d = x.getImageData(0, 0, c.width, c.height), px = d.data;
    const hue = (1 - p.h) * 2 * Math.PI, ch = Math.cos(hue), sh = Math.sin(hue);
    const k = p.s * p.v;
    for (let i = 0; i < px.length; i += 4) {
      if (px[i + 3] === 0) continue;
      const r = px[i] / 255, g = px[i + 1] / 255, b = px[i + 2] / 255;
      const Y = (YIQ[0][0] * r + YIQ[0][1] * g + YIQ[0][2] * b) * p.v;
      const I0 = YIQ[1][0] * r + YIQ[1][1] * g + YIQ[1][2] * b;
      const Q0 = YIQ[2][0] * r + YIQ[2][1] * g + YIQ[2][2] * b;
      const I = (I0 * ch - Q0 * sh) * k, Q = (I0 * sh + Q0 * ch) * k;
      px[i] = 255 * (INV[0][0] * Y + INV[0][1] * I + INV[0][2] * Q);
      px[i + 1] = 255 * (INV[1][0] * Y + INV[1][1] * I + INV[1][2] * Q);
      px[i + 2] = 255 * (INV[2][0] * Y + INV[2][1] * I + INV[2][2] * Q);
    }
    x.putImageData(d, 0, 0);
    hsvCache.set(key, c);
    return c;
  }
  const silhouetteCache = new Map();
  function silhouette(name) {
    if (silhouetteCache.has(name)) return silhouetteCache.get(name);
    const src = imgs[name], c = document.createElement('canvas');
    c.width = src.width; c.height = src.height;
    const x = c.getContext('2d');
    x.drawImage(src, 0, 0);
    x.globalCompositeOperation = 'source-in';
    x.fillStyle = '#000';
    x.fillRect(0, 0, c.width, c.height);
    silhouetteCache.set(name, c);
    return c;
  }

  // ---- TextureRect stretch modes --------------------------------------------------------------
  function drawContain(ctx, img, [x, y, w, h]) {          // STRETCH_KEEP_ASPECT_CENTERED
    const s = Math.min(w / img.width, h / img.height), dw = img.width * s, dh = img.height * s;
    ctx.drawImage(img, x + (w - dw) / 2, y + (h - dh) / 2, dw, dh);
  }
  function drawCover(ctx, img, [x, y, w, h]) {            // STRETCH_KEEP_ASPECT_COVERED
    const s = Math.max(w / img.width, h / img.height), sw = w / s, sh = h / s;
    ctx.drawImage(img, (img.width - sw) / 2, (img.height - sh) / 2, sw, sh, x, y, w, h);
  }
  function drawNinePatchH(ctx, img, x, y, w, h, ml, mr) { // NinePatchRect, horizontal margins only
    const iw = img.width, ih = img.height;
    ctx.drawImage(img, 0, 0, ml, ih, x, y, ml, h);
    ctx.drawImage(img, ml, 0, iw - ml - mr, ih, x + ml, y, w - ml - mr, h);
    ctx.drawImage(img, iw - mr, 0, mr, ih, x + w - mr, y, mr, h);
  }

  // ---- text (Godot Label look: shadow, then outline, then fill) -------------------------------
  // Godot's outline_size N strokes the glyph with radius N/4 (FreeType stroker, 26.6 units) — so a
  // canvas lineWidth of N/2. Matched against a Godot render of card.tscn.
  function font(size, bold) { return `${bold ? 700 : 400} ${size}px "FP Kreon"`; }
  // Godot line metrics for Kreon (hhea 997/-293 @ 1024 upm). Browsers use the OS/2 win metrics
  // instead, which would shift every label ~1.5px — so don't ask the canvas.
  function metrics(size) { return { asc: size * 997 / 1024, desc: size * 293 / 1024 }; }
  function drawText(ctx, text, x, baseline, o) {
    baseline -= 1.5;   // Godot's MSDF glyphs sit ~1.5 units higher than the browser rasterizer's (measured)
    ctx.font = font(o.size, o.bold);
    ctx.letterSpacing = (o.spacing || 0) + 'px';
    ctx.textAlign = 'left';
    ctx.textBaseline = 'alphabetic';
    ctx.lineJoin = 'round';
    if (o.shadow) {
      ctx.fillStyle = o.shadow.color;
      if (o.shadow.outline) { ctx.strokeStyle = o.shadow.color; ctx.lineWidth = o.shadow.outline / 2; ctx.strokeText(text, x + o.shadow.dx, baseline + o.shadow.dy); }
      ctx.fillText(text, x + o.shadow.dx, baseline + o.shadow.dy);
    }
    if (o.outline) { ctx.strokeStyle = o.outline.color; ctx.lineWidth = o.outline.size / 2; ctx.strokeText(text, x, baseline); }
    ctx.fillStyle = o.color;
    ctx.fillText(text, x, baseline);
    ctx.letterSpacing = '0px';
  }
  function textWidth(ctx, text, size, bold, spacing) {
    ctx.font = font(size, bold);
    ctx.letterSpacing = (spacing || 0) + 'px';
    const w = ctx.measureText(text).width;
    ctx.letterSpacing = '0px';
    return w;
  }
  // Label in a rect, centred both ways, MegaLabel auto-shrink to fit the width.
  function drawLabel(ctx, text, [x, y, w, h], o) {
    let size = o.size;
    while (size > (o.minSize || 8) && textWidth(ctx, text, size, o.bold, o.spacing) > w) size -= 1;
    const tw = textWidth(ctx, text, size, o.bold, o.spacing);
    const m = metrics(size);
    const top = y + (h - (m.asc + m.desc)) / 2;
    drawText(ctx, text, x + (w - tw) / 2, top + m.asc, { ...o, size });
  }

  // ---- description: tiny BBCode subset -> wrapped, centred rich text ---------------------------
  // [gold]..[/gold] (and the other StsColors tags), [E] energy icon, \n line breaks.
  function parseRich(src) {
    const runs = [];
    const stack = [COLORS.cream];
    const re = /\[(\/?)([a-z]+)\]|\[E\]|\n|[^\[\n]+|\[/g;
    let m;
    while ((m = re.exec(src))) {
      const t = m[0];
      if (t === '[E]') runs.push({ icon: true, color: stack[stack.length - 1] });
      else if (t === '\n') runs.push({ br: true });
      else if (m[2] && COLORS[m[2]]) { if (m[1]) { if (stack.length > 1) stack.pop(); } else stack.push(COLORS[m[2]]); }
      else runs.push({ text: t, color: stack[stack.length - 1] });
    }
    // split text runs into words (keep the spaces as their own tokens)
    const tokens = [];
    for (const r of runs) {
      if (!r.text) { tokens.push(r); continue; }
      for (const part of r.text.split(/( +)/)) if (part) tokens.push({ text: part, color: r.color, space: /^ +$/.test(part) });
    }
    return tokens;
  }
  // Godot RichTextLabel wrapping: a word moves down when it plus its trailing space overflows.
  // line_separation (-3) applies only between wrapped lines of one paragraph, not across a newline.
  function layoutRich(ctx, tokens, size, maxW) {
    const iconW = size * 1.05;
    const sized = tokens.map(t => t.br ? t : { ...t, w: t.icon ? iconW : textWidth(ctx, t.text, size, false) });
    const lines = [{ items: [], para: true }];
    let lineW = 0;
    sized.forEach((t, i) => {
      if (t.br) { lines.push({ items: [], para: true }); lineW = 0; return; }
      const line = lines[lines.length - 1];
      if (!t.space) {
        const next = sized[i + 1];
        const trail = next && next.space ? next.w : 0;
        if (lineW + t.w + trail > maxW && line.items.some(x => !x.space)) {
          while (line.items.length && line.items[line.items.length - 1].space) line.items.pop();
          lines.push({ items: [], para: false }); lineW = 0;
        }
      } else if (lines[lines.length - 1].items.length === 0) return;
      lines[lines.length - 1].items.push(t);
      lineW += t.w;
    });
    for (const l of lines) {
      while (l.items.length && l.items[l.items.length - 1].space) l.items.pop();
      l.w = l.items.reduce((a, t) => a + t.w, 0);
    }
    return lines;
  }
  function drawDescription(ctx, src, rect, styleId) {
    const [x, y, w, h] = rect;
    const tokens = parseRich(src);
    let size = 21, lines, m, total;
    for (; size >= 12; size--) {                                  // MegaRichTextLabel auto-shrink 21 -> 12
      lines = layoutRich(ctx, tokens, size, w);
      m = metrics(size);
      const lh = m.asc + m.desc;
      total = lines.reduce((a, l, i) => a + lh - (i > 0 && !l.para ? 3 : 0), 0);
      if (total <= h && lines.every(l => l.w <= w)) break;
    }
    const lh = m.asc + m.desc;
    let top = y + (h - total) / 2;
    const icon = imgs['textEnergy_' + styleId];
    lines.forEach((line, i) => {
      if (i > 0) top += lh - (line.para ? 0 : 3);
      let cx = x + (w - line.w) / 2;
      const base = top + m.asc;
      for (const t of line.items) {
        if (t.icon) {
          const s = size * 1.05;
          ctx.globalAlpha = 0.25; ctx.filter = 'brightness(0)';
          ctx.drawImage(icon, cx + 2, base - s * 0.8 + 2, s, s);
          ctx.globalAlpha = 1; ctx.filter = 'none';
          ctx.drawImage(icon, cx, base - s * 0.8, s, s);
        } else if (!t.space) {
          drawText(ctx, t.text, cx, base, { size, color: t.color, shadow: { color: 'rgba(0,0,0,0.251)', dx: 2, dy: 2 } });
        }
        cx += t.w;
      }
    });
  }

  // ---- portrait processing (mirrors tools/pack-atlases.py flatten_resize) -----------------------
  // The packer stretches ANY source to exactly 250x190 over opaque black. `ingame` reproduces that
  // slice (what players actually see); otherwise the source is drawn at full detail into the slot.
  // Landscape art packs to 250x190; taller-than-wide art packs to the 250x351 full-art slice.
  function portraitSlice(img) {
    if (img._fpSlice) return img._fpSlice;
    const tall = (img.naturalHeight || img.height) > (img.naturalWidth || img.width);
    const W = 250, H = tall ? 351 : 190;
    // step-halving downscale ~ LANCZOS quality
    let cur = img, w = img.naturalWidth || img.width, h = img.naturalHeight || img.height;
    while (w / 2 >= W * 1.5 && h / 2 >= H * 1.5) {
      const c = document.createElement('canvas');
      c.width = Math.round(w / 2); c.height = Math.round(h / 2);
      const x = c.getContext('2d'); x.imageSmoothingQuality = 'high';
      x.drawImage(cur, 0, 0, c.width, c.height);
      cur = c; w = c.width; h = c.height;
    }
    const out = document.createElement('canvas');
    out.width = W; out.height = H;
    const x = out.getContext('2d');
    x.fillStyle = '#000'; x.fillRect(0, 0, W, H);
    x.imageSmoothingQuality = 'high';
    x.drawImage(cur, 0, 0, W, H);
    img._fpSlice = out;
    return out;
  }

  // ---- the card ---------------------------------------------------------------------------------
  // opts: { style, type, rarity, title, cost ('' = none, 'X', '0'..), desc, upgraded, art (Image|null),
  //         ingame (bool), xray (bool), shadow (bool), scale (px per card unit) }
  function renderCard(canvas, opts) {
    const scale = opts.scale || 2;
    const B = L.bounds;
    canvas.width = Math.round(B.w * scale);
    canvas.height = Math.round(B.h * scale);
    const ctx = canvas.getContext('2d');
    ctx.setTransform(scale, 0, 0, scale, -B.x * scale, -B.y * scale);
    ctx.imageSmoothingEnabled = true;
    ctx.imageSmoothingQuality = 'high';

    const style = A.styles.find(s => s.id === opts.style) || A.styles[0];
    const rarity = A.rarities.find(r => r.id === opts.rarity) || A.rarities[0];
    const frameKey = 'frame_' + FRAME_OF[opts.type];
    const borderKey = 'border_' + FRAME_OF[opts.type];

    if (opts.shadow !== false) {
      ctx.globalAlpha = 0.251;
      drawContain(ctx, silhouette(frameKey), L.shadow);
      ctx.globalAlpha = 1;
    }
    // x-ray: frame + portrait border at partial opacity, so the art they cover shows through
    const layerAlpha = opts.xray ? 0.45 : 1;
    if (opts.rarity === 'ancient') { drawAncientBody(ctx, opts, scale, layerAlpha); return drawCardTop(ctx, opts, style, rarity, scale); }
    // portrait (Portrait: KEEP_ASPECT_CENTERED, so a full-art slice letterboxes in a normal frame)
    if (opts.art) {
      if (opts.ingame) drawContain(ctx, portraitSlice(opts.art), L.portrait);
      else ctx.drawImage(opts.art, ...L.portrait);
    } else drawEmptyPortrait(ctx, L.portrait, opts.emptyText);
    ctx.globalAlpha = layerAlpha;
    drawContain(ctx, hsv(frameKey, style.hsv), L.frame);
    ctx.globalAlpha = 1;

    drawDescription(ctx, opts.desc || '', L.desc, style.id);

    ctx.globalAlpha = layerAlpha;
    drawContain(ctx, hsv(borderKey, rarity.hsv), L.border);
    ctx.globalAlpha = 1;
    drawCover(ctx, hsv('banner', rarity.hsv), L.banner);
    return drawCardTop(ctx, opts, style, rarity, scale);
  }

  // Ancient rarity: no Frame / PortraitBorder / TitleBanner; a masked full-height portrait under a glass
  // overlay, an additive border, a dark text panel and the Ancient banner (baked by build-frame-preview.py).
  function drawAncientBody(ctx, opts, scale, layerAlpha) {
    const [x, y, w, h] = L.ancientPortrait;
    const off = document.createElement('canvas');
    off.width = Math.round(w * scale); off.height = Math.round(h * scale);
    const o = off.getContext('2d');
    o.imageSmoothingQuality = 'high';
    if (opts.art) o.drawImage(opts.ingame ? portraitSlice(opts.art) : opts.art, 0, 0, off.width, off.height);
    else { o.setTransform(scale, 0, 0, scale, -x * scale, -y * scale); drawEmptyPortrait(o, [x + 24, y + 30, w - 48, 190], opts.emptyText || ['Drop art here', 'full-art: taller than wide']); o.setTransform(1, 0, 0, 1, 0, 0); }
    o.globalCompositeOperation = 'destination-in';
    o.drawImage(imgs.ancient_mask, 0, 0, off.width, off.height);
    ctx.drawImage(off, x, y, w, h);
    ctx.globalAlpha = layerAlpha;
    ctx.globalCompositeOperation = 'lighter';
    ctx.drawImage(imgs.ancient_glass_add, ...L.ancientGlass);
    ctx.globalCompositeOperation = 'source-over';
    ctx.drawImage(imgs.ancient_glass_white, ...L.ancientGlass);
    ctx.globalCompositeOperation = 'lighter';
    drawContain(ctx, imgs.ancient_border, L.ancientBorder);
    ctx.globalCompositeOperation = 'source-over';
    ctx.globalAlpha = 0.66 * layerAlpha;
    drawContain(ctx, silhouette('ancient_textbg_' + FRAME_OF[opts.type]), L.ancientTextBg);
    ctx.globalAlpha = 1;
    drawDescription(ctx, opts.desc || '', L.desc, opts.style);
    drawCover(ctx, imgs.ancient_banner, L.banner);
    const f = imgs.ancient_flame, F = L.ancientFlame;
    ctx.drawImage(f, F.x - f.width * F.scale / 2, F.y - f.height * F.scale / 2, f.width * F.scale, f.height * F.scale);
  }

  // Title, type plaque and energy cost — shared by every frame.
  function drawCardTop(ctx, opts, style, rarity, scale) {
    const canvas = ctx.canvas;
    const titleText = (opts.title || '') + (opts.upgraded ? '+' : '');
    drawLabel(ctx, titleText, L.title, {
      size: 26, minSize: 12, spacing: 1, bold: false,
      color: opts.upgraded ? COLORS.green : COLORS.cream,
      outline: { size: 12, color: opts.upgraded ? COLORS.titleUpgradedOutline : '#' + rarity.outline },
      shadow: { color: 'rgba(0,0,0,0.188)', dx: 2, dy: 2, outline: 12 },
    });

    // type plaque (NCard.UpdateTypePlaqueSizeAndPosition)
    const P = L.plaque;
    const typeText = opts.typeLabel || opts.type.charAt(0).toUpperCase() + opts.type.slice(1);
    const labelW = Math.max(P.labelMinW, textWidth(ctx, typeText, 16, true));
    const plaqueW = Math.max(labelW + P.pad, P.minW);
    drawNinePatchH(ctx, hsv('plaque', rarity.hsv), -plaqueW / 2, P.y, plaqueW, P.h, P.marginL, P.marginR);
    drawLabel(ctx, typeText, [-labelW / 2, P.y + P.h / 2 - 14, labelW, 28], { size: 16, minSize: 10, bold: true, color: 'rgba(0,0,0,0.753)' });

    // energy cost
    if (opts.cost !== '' && opts.cost != null) {
      drawContain(ctx, imgs['energy_' + style.id], L.energy);
      drawLabel(ctx, String(opts.cost), L.energyLabel, {
        size: 32, minSize: 22, bold: true, color: COLORS.cream,
        outline: { size: 16, color: COLORS.energyOutline },
        shadow: { color: 'rgba(0,0,0,0.188)', dx: 2, dy: 2, outline: 16 },
      });
    }
    return canvas;
  }

  function drawEmptyPortrait(ctx, [x, y, w, h], text) {
    const g = ctx.createLinearGradient(x, y, x, y + h);
    g.addColorStop(0, '#3b2b2e'); g.addColorStop(1, '#1d1517');
    ctx.fillStyle = g;
    ctx.fillRect(x, y, w, h);
    ctx.save();
    ctx.strokeStyle = 'rgba(232,220,200,0.35)';
    ctx.setLineDash([6, 5]);
    ctx.lineWidth = 1.5;
    ctx.strokeRect(x + 16, y + 14, w - 32, h - 40);
    ctx.restore();
    const [main, sub] = text || ['Drop art here', '1000 × 760 recommended'];
    drawLabel(ctx, main, [x, y + 40, w, 40], { size: 20, bold: true, color: 'rgba(232,220,200,0.7)' });
    if (sub) drawLabel(ctx, sub, [x, y + 72, w, 30], { size: 14, color: 'rgba(232,220,200,0.45)' });
  }

  // Representative colour of a recoloured layer, for UI swatches.
  function swatch(kind, id) {
    const p = (kind === 'style' ? A.styles : A.rarities).find(x => x.id === id).hsv;
    const c = hsv(kind === 'style' ? 'frame_skill' : 'banner', p);
    const [x, y] = kind === 'style' ? [24, 420] : [327, 60];   // frame side rail / banner middle
    const d = c.getContext('2d', { willReadFrequently: true }).getImageData(x, y, 1, 1).data;
    return `rgb(${d[0]},${d[1]},${d[2]})`;
  }

  // localization string -> preview markup (SmartFormat subset used by the mods). `numbers` is a card's
  // cards.json numbers map, keyed by var name with any "Power" suffix stripped (regen.js convention).
  // `self` is the value a nested `{:diff()}` refers to (inside a plural option).
  function locToMarkup(src, numbers, upgraded, self) {
    const nums = numbers || {};
    const val = name => name === '' ? self : nums[name] ?? nums[name.replace(/Power$/, '')] ??
      (/^Calculated/.test(name) ? nums.CalculationBase : undefined);   // live-scaling vars show their base
    let out = '', i = 0;
    while (i < src.length) {
      if (src[i] !== '{') { out += src[i++]; continue; }
      let depth = 0, j = i;
      for (; j < src.length; j++) { if (src[j] === '{') depth++; else if (src[j] === '}' && --depth === 0) break; }
      const body = src.slice(i + 1, j);
      i = j + 1;
      const parts = splitTop(body, ':');
      const name = parts[0], fmt = parts[1] || '';
      const v = val(name);
      if (name === 'IfUpgraded' && fmt === 'show') { const [a, b = ''] = splitTop(parts.slice(2).join(':'), '|'); out += locToMarkup(upgraded ? a : b, nums, upgraded, self); }
      else if (name === 'InCombat') { /* combat-only line */ }
      else if (fmt.startsWith('energyIcons')) { const n = Number(v ?? 1); out += n > 0 && n <= 3 ? '[E]'.repeat(n) : `${n}[E]`; }
      else if (fmt === 'plural') { const [one, many = one] = splitTop(parts.slice(2).join(':'), '|'); out += locToMarkup(Number(v) === 1 ? one : many, nums, upgraded, v); }
      else if (fmt === 'cond') { /* conditional extra — skipped */ }
      else out += v != null ? String(v) : '?';   // a var regen.js didn't capture
    }
    return out;
  }
  function splitTop(s, sep) {
    const out = []; let depth = 0, cur = '';
    for (const ch of s) {
      if (ch === '{') depth++; else if (ch === '}') depth--;
      if (ch === sep && depth === 0) { out.push(cur); cur = ''; } else cur += ch;
    }
    out.push(cur);
    return out;
  }
  window.FramePreview = { ready, renderCard, swatch, locToMarkup, layout: L, styles: A.styles, rarities: A.rarities, generated: A.generated };
})();
