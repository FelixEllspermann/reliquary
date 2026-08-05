// extra-summon.jsx — RELIQUARY: summoning a Reliquary monster from the Extra Deck.
// The vault is the summon: three rings turn into alignment, four locks bite, the
// plate splits into quadrants, and the card comes up out of the light.

const { SceneStage, useScene } = window;

const P = {
  gold: '#C8A45C', light: '#EBCE8A', pale: '#F8EED6', dark: '#7A5A1E',
  parch1: '#EBE1C7', parch2: '#D9CCAB',
  ember: '#E0603A', emberLit: '#F3C3A6', teal: '#8FC6D2',
  vault: '#EFE7FA', vaultMid: '#B9A3E0', vaultDeep: '#5E4E8C', vaultDark: '#241C3C',
  good: '#7ACD96', dim: '#9C8A6A', muted: '#A2917A',
};

const c01 = (v) => (v < 0 ? 0 : v > 1 ? 1 : v);
const seg = (p, a, b) => c01((p - a) / (b - a));
const mix = (a, b, t) => a + (b - a) * t;
const hexA = (hex, a) => {
  const h = hex.replace('#', '');
  return `rgba(${parseInt(h.slice(0, 2), 16)},${parseInt(h.slice(2, 4), 16)},${parseInt(h.slice(4, 6), 16)},${a})`;
};

const MOTION = {
  enter: (p) => 1 - Math.pow(1 - c01(p), 3),
  drift: (p) => 0.5 - 0.5 * Math.cos(Math.PI * c01(p)),
  pop: (p) => { const t = c01(p), s = 1.9; return 1 + (s + 1) * Math.pow(t - 1, 3) + s * Math.pow(t - 1, 2); },
};

const W = 1280, H = 720;
const VAULT = { x: 640, y: 402 };  // centred so a 45deg-aligned ring fits under the label band
const ZONE = { x: 568, y: 452 };          // where the monster ends up
const EXTRA = { x: 906, y: 452 };         // the extra deck slot
const FCW = 132, FCH = 185;               // field card
const HCW = 216, HCH = 302;               // hero card
const cfg = { rings: 1, settle: true };

/* ---------------- field ---------------- */
function Field({ dim = 0, shake = 0, wash = 0 }) {
  const sx = Math.sin(shake * Math.PI * 12) * shake * 6;
  const sy = Math.cos(shake * Math.PI * 9) * shake * 4;
  return (
    <div style={{ position: 'absolute', inset: 0, overflow: 'hidden', transform: `translate(${sx}px,${sy}px)` }}>
      <div style={{ position: 'absolute', inset: -40, background: 'radial-gradient(ellipse 980px 620px at 50% 48%, #2A1C12, #0A0705 78%)' }} />
      <div style={{
        position: 'absolute', inset: 0,
        background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.04) 0 1px,transparent 1px 26px),repeating-linear-gradient(-45deg,rgba(200,164,92,.04) 0 1px,transparent 1px 26px)',
      }} />
      <div style={{ position: 'absolute', left: 0, right: 0, top: 316, height: 1, background: hexA(P.gold, 0.2) }} />
      {[178, 452].map((y) => [372, 470, 568, 666, 764].map((x, i) => (
        <div key={y + '-' + i} style={{
          position: 'absolute', left: x, top: y, width: FCW + 8, height: FCH + 8,
          transform: 'translate(-50%,-50%)', boxSizing: 'border-box', border: `1px solid ${hexA(P.gold, 0.13)}`,
        }} />
      )))}
      <div style={{ position: 'absolute', inset: 0, boxShadow: 'inset 0 0 190px rgba(0,0,0,.9)' }} />
      {dim > 0 && <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: dim * 0.62 }} />}
      {wash > 0 && <div style={{ position: 'absolute', inset: 0, background: P.pale, opacity: wash }} />}
    </div>
  );
}

/* the extra deck slot — where the summon is paid from */
function ExtraSlot({ glow = 0, count = 3, caption = 1 }) {
  return (
    <div style={{ position: 'absolute', left: EXTRA.x, top: EXTRA.y, transform: 'translate(-50%,-50%)' }}>
      <div style={{
        position: 'absolute', left: 0, top: 0, width: FCW * 2, height: FCH * 1.3,
        transform: 'translate(-50%,-50%)', borderRadius: '50%',
        background: `radial-gradient(ellipse at 50% 50%, ${hexA(P.vaultMid, 0.1 + glow * 0.36)}, transparent 66%)`,
      }} />
      {Array.from({ length: count }, (_, i) => (
        <div key={i} style={{
          position: 'absolute', left: 0, top: -i * 3, transform: `translate(-50%,-50%) rotate(${(i % 2 ? 1 : -1) * (1.4 + i * 0.6)}deg)`,
          width: FCW, height: FCH, borderRadius: 5, overflow: 'hidden',
          background: 'radial-gradient(ellipse at 50% 50%, #2A2148, #0D0916 78%)',
          border: `2px solid ${hexA(P.vaultMid, 0.6 + glow * 0.4)}`,
          filter: 'drop-shadow(0 8px 18px rgba(0,0,0,.75))',
        }}>
          <div style={{
            position: 'absolute', inset: 0,
            background: 'repeating-linear-gradient(45deg,rgba(185,163,224,.14) 0 1px,transparent 1px 10px),repeating-linear-gradient(-45deg,rgba(185,163,224,.14) 0 1px,transparent 1px 10px)',
          }} />
          <div style={{
            position: 'absolute', left: '50%', top: '50%', width: 54, height: 54,
            transform: 'translate(-50%,-50%) rotate(45deg)', border: `2px solid ${hexA(P.vaultMid, 0.55)}`,
          }} />
        </div>
      ))}
      <div style={{ position: 'absolute', left: 0, top: FCH * 0.62, transform: 'translateX(-50%)', whiteSpace: 'nowrap', textAlign: 'center', opacity: caption }}>
        <span style={{ font: `500 10px/1 'Oswald',sans-serif`, letterSpacing: '.26em', color: hexA(P.vaultMid, 0.5 + glow * 0.5) }}>EXTRA DECK</span>
      </div>
    </div>
  );
}

/* ---------------- the vault ----------------
   Three diamond rings that arrive at alignment, four locks that bite inward, and
   a plate that splits into quadrants. `align` 0 = scattered, 1 = locked;
   `open` 0 = shut, 1 = the quadrants are clear of the frame. */
function Vault({ align = 0, open = 0, glow = 0, fade = 1, r = 250 }) {
  if (fade <= 0.001) return null;
  const a = MOTION.enter(align);
  const o = MOTION.enter(open);
  const RINGS = [
    { s: 1.0, off: -46, sw: 3, c: P.vault },
    { s: 0.74, off: 38, sw: 2, c: P.vaultMid },
    { s: 0.5, off: -62, sw: 2, c: P.vault },
  ];
  return (
    <div style={{
      position: 'absolute', left: VAULT.x, top: VAULT.y, width: 0, height: 0, opacity: fade,
      filter: `drop-shadow(0 0 ${20 + glow * 60}px ${hexA(P.vaultMid, 0.25 + glow * 0.5)})`,
    }}>
      {/* light behind the door */}
      <div style={{
        position: 'absolute', left: 0, top: 0, width: r * 2.4 * (0.4 + o * 1.1), height: r * 2.4 * (0.4 + o * 1.1),
        transform: 'translate(-50%,-50%)', borderRadius: '50%',
        background: `radial-gradient(circle, ${hexA(P.pale, 0.2 * a + o * 0.62)}, ${hexA(P.vaultMid, 0.16 * a + o * 0.22)} 40%, transparent 66%)`,
      }} />
      {/* the four door quadrants, cut from one diamond plate */}
      {[[-1, -1], [1, -1], [-1, 1], [1, 1]].map(([dx, dy], i) => (
        <div key={i} style={{
          position: 'absolute', left: 0, top: 0, width: r * 2, height: r * 2,
          transform: `translate(calc(-50% + ${dx * o * r * 1.15}px), calc(-50% + ${dy * o * r * 1.15}px)) rotate(${o * dx * 12}deg)`,
          clipPath: `inset(${dy < 0 ? 0 : 50}% ${dx > 0 ? 0 : 50}% ${dy > 0 ? 0 : 50}% ${dx < 0 ? 0 : 50}%)`,
          opacity: 1 - c01(open / 0.86),
        }}>
          <div style={{
            position: 'absolute', left: '50%', top: '50%', width: r * 1.28, height: r * 1.28,
            transform: 'translate(-50%,-50%) rotate(45deg)',
            background: `linear-gradient(135deg,${hexA(P.vaultDark, 0.96)},${hexA('#0A0705', 0.98)})`,
            border: `3px solid ${P.vaultMid}`,
            boxShadow: `inset 0 0 40px rgba(0,0,0,.8)`,
          }}>
            <div style={{
              position: 'absolute', inset: 0,
              background: 'repeating-linear-gradient(45deg,rgba(185,163,224,.1) 0 1px,transparent 1px 14px),repeating-linear-gradient(-45deg,rgba(185,163,224,.1) 0 1px,transparent 1px 14px)',
            }} />
            <div style={{ position: 'absolute', inset: '18%', border: `1px solid ${hexA(P.vaultMid, 0.4)}` }} />
          </div>
        </div>
      ))}
      {/* rings turning into alignment */}
      {RINGS.map((ring, i) => (
        <div key={'r' + i} style={{
          position: 'absolute', left: 0, top: 0, width: r * 2 * ring.s, height: r * 2 * ring.s,
          transform: `translate(-50%,-50%) rotate(${45 + mix(ring.off, 0, a) * cfg.rings}deg) scale(${1 + o * (0.6 + i * 0.3)})`,
          border: `${ring.sw}px solid ${hexA(ring.c, mix(0.35, 0.95, a) * (1 - c01(open / 0.9)))}`,
        }} />
      ))}
      {/* four locks biting inward */}
      {[[0, -1], [1, 0], [0, 1], [-1, 0]].map(([dx, dy], i) => {
        const d = mix(r * 1.5, r * 0.98, a);
        return (
          <div key={'l' + i} style={{
            position: 'absolute', left: dx * d, top: dy * d, width: 22, height: 22,
            transform: `translate(-50%,-50%) rotate(45deg) scale(${mix(0.5, 1, a)})`,
            background: `linear-gradient(135deg,${P.vault},${P.vaultDeep})`,
            boxShadow: `0 0 ${10 + a * 20}px ${hexA(P.vaultMid, 0.7)}`,
            opacity: (0.4 + a * 0.6) * (1 - c01(open / 0.7)),
          }} />
        );
      })}
      {/* the gold core of the seal, which is what the card comes out of */}
      <div style={{
        position: 'absolute', left: 0, top: 0,
        width: mix(28, 74, a) * (1 + o * 1.4), height: mix(28, 74, a) * (1 + o * 1.4),
        transform: 'translate(-50%,-50%) rotate(45deg)',
        background: `linear-gradient(135deg,${P.pale},${P.dark})`,
        boxShadow: `0 0 ${20 + a * 30 + o * 60}px ${hexA(P.pale, 0.6)}`,
        opacity: 1 - c01(open / 0.8),
      }} />
    </div>
  );
}

/* ---------------- the summoned card ---------------- */
const CARD = { kind: 'RELIQUARY', attr: 'LIGHT', attrC: '#F3DDA4', type: 'MYTH', lv: 3, dmg: '3200', def: '2800' };

function Back({ w, h }) {
  return (
    <div style={{
      position: 'absolute', inset: 0, borderRadius: 7, overflow: 'hidden', backfaceVisibility: 'hidden',
      background: 'radial-gradient(ellipse at 50% 50%, #2A2148, #0D0916 78%)', border: `2px solid ${P.vaultMid}`,
    }}>
      <div style={{
        position: 'absolute', inset: 0,
        background: 'repeating-linear-gradient(45deg,rgba(185,163,224,.14) 0 1px,transparent 1px 11px),repeating-linear-gradient(-45deg,rgba(185,163,224,.14) 0 1px,transparent 1px 11px)',
      }} />
      <div style={{ position: 'absolute', inset: 5, border: `1px solid ${hexA(P.vaultMid, 0.5)}`, borderRadius: 3 }} />
      <div style={{ position: 'absolute', left: '50%', top: '50%', width: w * 0.44, height: w * 0.44, transform: 'translate(-50%,-50%) rotate(45deg)', border: `2px solid ${hexA(P.vaultMid, 0.6)}` }} />
      <div style={{ position: 'absolute', left: '50%', top: '50%', width: w * 0.2, height: w * 0.2, transform: 'translate(-50%,-50%) rotate(45deg)', background: `linear-gradient(135deg,${P.vault},${P.vaultDeep})` }} />
    </div>
  );
}

function Front({ w, h, halo = 0 }) {
  const pad = w * 0.088, inner = w - pad * 2;
  return (
    <div style={{
      position: 'absolute', inset: 0, borderRadius: 7, overflow: 'hidden', backfaceVisibility: 'hidden',
      transform: 'rotateY(180deg)', boxSizing: 'border-box', padding: `${h * 0.028}px ${pad}px`,
      background: 'linear-gradient(165deg,#332315,#150d07 55%,#251809)',
      border: `3px solid ${P.vault}`,
      boxShadow: halo > 0.02 ? `inset 0 0 ${26 * halo}px ${hexA(P.vault, 0.4 * halo)}` : 'none',
    }}>
      <div style={{ position: 'absolute', inset: 4, border: `1px solid ${hexA(P.vaultMid, 0.45)}`, borderRadius: 4 }} />
      {[['left', 'top'], ['right', 'top'], ['left', 'bottom'], ['right', 'bottom']].map(([a, b]) => (
        <div key={a + b} style={{ position: 'absolute', [a]: 9, [b]: 9, width: 9, height: 9, transform: 'rotate(45deg)', background: P.vault }} />
      ))}
      <div style={{ position: 'relative', width: inner, height: '100%', display: 'flex', flexDirection: 'column', gap: h * 0.013 }}>
        <div style={{ height: h * 0.082, flex: 'none', display: 'flex', alignItems: 'center', gap: 4 }}>
          <div style={{
            flex: 1, height: '100%', display: 'flex', alignItems: 'center', padding: '0 9px', boxSizing: 'border-box',
            background: `linear-gradient(180deg,#3A2B4E,#1C1428)`,
            borderTop: `1px solid ${P.vaultMid}`, borderBottom: `1px solid ${P.vaultMid}`,
            clipPath: 'polygon(0 0,100% 0,calc(100% - 8px) 100%,8px 100%)', overflow: 'hidden',
          }}>
            <div style={{ width: '74%', height: 5, background: `linear-gradient(90deg,${hexA(P.vault, 0.9)},${hexA(P.vaultDeep, 0.4)})` }} />
          </div>
          <div style={{
            position: 'relative', width: h * 0.072, height: h * 0.082, flex: 'none',
            background: `linear-gradient(160deg,${P.vault},${P.vaultDeep})`,
            clipPath: 'polygon(50% 0,100% 20%,100% 66%,50% 100%,0 66%,0 20%)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
          }}>
            <div style={{ position: 'absolute', inset: 2, background: 'linear-gradient(160deg,#241C3C,#0D0916)', clipPath: 'polygon(50% 0,100% 20%,100% 66%,50% 100%,0 66%,0 20%)' }} />
            <span style={{ position: 'relative', font: `700 ${h * 0.045}px/1.2 'Cinzel',serif`, color: P.vault }}>{CARD.lv}</span>
          </div>
        </div>
        <div style={{
          flex: '1 1 auto', minHeight: 0, aspectRatio: '1', alignSelf: 'center',
          boxSizing: 'border-box', padding: 5,
          background: `linear-gradient(160deg,#3E2C16,#1A1108)`, border: `2px solid ${P.gold}`,
        }}>
          <div style={{ width: '100%', height: '100%', boxSizing: 'border-box', overflow: 'hidden', border: `1px solid ${hexA(P.gold, 0.65)}` }}>
            <img src="uploads/artwork-1785612438938.png" alt="" style={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }} />
          </div>
        </div>
        <div style={{ height: h * 0.05, flex: 'none', display: 'flex', gap: 2 }}>
          <div style={{
            flex: 'none', padding: '0 6px', display: 'flex', alignItems: 'center',
            background: `linear-gradient(180deg,${P.vault},${P.vaultDeep})`, color: '#1B1428',
            font: `600 ${h * 0.026}px/1 'Oswald',sans-serif`, letterSpacing: '.14em',
          }}>{CARD.kind}</div>
          <div style={{
            flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '0 6px',
            background: 'rgba(0,0,0,.35)', border: `1px solid ${hexA(P.vaultMid, 0.45)}`,
          }}>
            <span style={{ display: 'flex', alignItems: 'center', gap: 4, font: `500 ${h * 0.026}px/1 'Oswald',sans-serif`, letterSpacing: '.12em', color: '#E4D3AE' }}>
              <i style={{ width: 5, height: 5, display: 'block', background: CARD.attrC, transform: 'rotate(45deg)' }} />{CARD.attr}
            </span>
            <span style={{ font: `500 ${h * 0.026}px/1 'Oswald',sans-serif`, letterSpacing: '.12em', color: '#B5A484' }}>{CARD.type}</span>
          </div>
        </div>
        <div style={{
          height: h * 0.15, flex: 'none', boxSizing: 'border-box', padding: '5px 7px',
          background: `linear-gradient(180deg,${P.parch1},${P.parch2})`, border: `1px solid #8C7440`,
          display: 'flex', flexDirection: 'column', gap: 3,
        }}>
          {['95%', '88%', '92%', '60%'].map((wv, i) => (
            <div key={i} style={{ width: wv, height: 3, background: 'rgba(46,36,23,.32)' }} />
          ))}
        </div>
        <div style={{ height: h * 0.066, flex: 'none', display: 'flex', gap: 3 }}>
          <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 5, background: 'rgba(0,0,0,.4)', border: `1px solid ${hexA(P.ember, 0.5)}` }}>
            <span style={{ font: `500 ${h * 0.024}px/1 'Oswald',sans-serif`, letterSpacing: '.12em', color: '#C08D74' }}>DMG</span>
            <span style={{ font: `700 ${h * 0.038}px/1.2 'Cinzel',serif`, color: P.emberLit }}>{CARD.dmg}</span>
          </div>
          <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 5, background: 'rgba(0,0,0,.4)', border: `1px solid ${hexA(P.teal, 0.5)}` }}>
            <span style={{ font: `500 ${h * 0.024}px/1 'Oswald',sans-serif`, letterSpacing: '.12em', color: '#7FAAB4' }}>DEF</span>
            <span style={{ font: `700 ${h * 0.038}px/1.2 'Cinzel',serif`, color: '#B9E6F0' }}>{CARD.def}</span>
          </div>
        </div>
      </div>
    </div>
  );
}

function Card({ x, y, w, h, flip = 0, scale = 1, rot = 0, glow = 0, o = 1 }) {
  if (o <= 0.001) return null;
  return (
    <div style={{
      position: 'absolute', left: x, top: y, width: w, height: h,
      transform: `translate(-50%,-50%) rotate(${rot}deg) scale(${scale})`,
      perspective: 1600, opacity: o,
      filter: `drop-shadow(0 18px 40px rgba(0,0,0,.82)) drop-shadow(0 0 ${16 + glow * 46}px ${hexA(P.vault, 0.2 + glow * 0.45)})`,
    }}>
      <div style={{ position: 'relative', width: '100%', height: '100%', transformStyle: 'preserve-3d', transform: `rotateY(${MOTION.drift(flip) * 180}deg)` }}>
        <Back w={w} h={h} />
        <Front w={w} h={h} halo={glow} />
      </div>
    </div>
  );
}

/* ---------------- furniture ---------------- */
function Label({ text, sub, o, rise, tone }) {
  if (o <= 0.001) return null;
  return (
    <div style={{
      position: 'absolute', left: 0, right: 0, top: 42,
      display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 12,
      opacity: o, transform: `translateY(${rise}px)`,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
        <div style={{ width: 74, height: 1, background: `linear-gradient(90deg,transparent,${tone})` }} />
        <span style={{ font: `500 13px/1 'Oswald',sans-serif`, letterSpacing: '.42em', color: tone, whiteSpace: 'nowrap' }}>{text}</span>
        <div style={{ width: 74, height: 1, background: `linear-gradient(270deg,transparent,${tone})` }} />
      </div>
      {sub && <span style={{ font: `400 17px/1 'Spectral',serif`, color: P.muted, whiteSpace: 'nowrap' }}>{sub}</span>}
    </div>
  );
}

function Chip({ text, tone }) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 10, padding: '11px 18px',
      background: 'rgba(0,0,0,.55)', border: `1px solid ${hexA(tone, 0.55)}`, whiteSpace: 'nowrap',
    }}>
      <i style={{ width: 8, height: 8, display: 'block', background: tone, transform: 'rotate(45deg)' }} />
      <span style={{ font: `400 15px/1 'Spectral',serif`, color: '#C8B189' }}>{text}</span>
    </div>
  );
}

/* motes drawn up into the vault — fixed offsets, reproducible on export */
function Motes({ amount, t, toward = true }) {
  if (amount <= 0.01) return null;
  return Array.from({ length: 14 }, (_, i) => {
    const ph = (t * 0.9 + i * 0.0714) % 1;
    const ang = (i / 14) * Math.PI * 2 + 0.5;
    const d = toward ? mix(300, 40, ph) : mix(40, 300, ph);
    const s = 4 + (i % 3);
    return (
      <div key={i} style={{
        position: 'absolute', left: VAULT.x + Math.cos(ang) * d, top: VAULT.y + Math.sin(ang) * d * 0.8,
        width: s, height: s, transform: 'rotate(45deg)',
        background: i % 3 ? P.vaultMid : P.pale,
        opacity: Math.sin(Math.PI * ph) * 0.8 * amount,
      }} />
    );
  });
}

/* ================= SCENES =================
   ┌───────────┬────────┬────────┬────────────────┬───────┐
   │ scene     │ align  │ open   │ card           │ vault │
   ├───────────┼────────┼────────┼────────────────┼───────┤
   │ Call      │ 0      │ 0      │ —              │ 0 → 1 │
   │ Unlock    │ 0 → 1  │ 0      │ —              │ 1     │
   │ Open      │ 1      │ 0 → 1  │ —              │ 1 → 0 │
   │ Emerge    │ 1      │ 1      │ rises, flips   │ 0     │
   │ Present   │ 1      │ 1      │ hero → zone    │ 0     │
   └───────────┴────────┴────────┴────────────────┴───────┘ */

function SceneCall() {
  const { progress: p } = useScene();
  const inn = MOTION.enter(seg(p, 0.04, 0.3));
  const born = MOTION.enter(seg(p, 0.3, 0.86));
  const pull = seg(p, 0.36, 1);
  return (
    <>
      <Field dim={MOTION.enter(seg(p, 0.24, 1)) * 0.7} />
      <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: 1 - MOTION.enter(seg(p, 0, 0.2)) }} />
      <ExtraSlot glow={inn * (0.3 + MOTION.enter(seg(p, 0.2, 0.8)) * 0.7)} />
      {/* the summon line: the extra deck feeds the vault */}
      <div style={{
        position: 'absolute', left: EXTRA.x, top: EXTRA.y, height: 2,
        width: Math.sqrt((EXTRA.x - VAULT.x) ** 2 + (EXTRA.y - VAULT.y) ** 2) * pull,
        transformOrigin: '0 50%',
        transform: `rotate(${Math.atan2(VAULT.y - EXTRA.y, VAULT.x - EXTRA.x) * 180 / Math.PI}deg)`,
        background: `linear-gradient(90deg,${hexA(P.vaultMid, 0.15)},${hexA(P.vault, 0.85)})`,
        boxShadow: `0 0 12px ${hexA(P.vaultMid, 0.7)}`,
        opacity: pull > 0 ? 1 - c01(seg(p, 0.9, 1)) : 0,
      }} />
      <Vault align={0} open={0} glow={born * 0.4} fade={born} r={213} />
      <Motes amount={born * 0.7} t={p} />
      <Label text="SUMMONING" sub="Level 3 · two tributes paid" o={inn} rise={mix(12, 0, inn)} tone={P.vaultMid} />
    </>
  );
}

function SceneUnlock() {
  const { progress: p } = useScene();
  const align = seg(p, 0.06, 0.82);
  const bite = MOTION.pop(seg(p, 0.62, 0.94));
  return (
    <>
      <Field dim={0.7} shake={Math.sin(Math.PI * seg(p, 0.78, 1)) * 0.35} />
      <ExtraSlot glow={1} count={2} />
      <Vault align={align} open={0} glow={0.4 + MOTION.enter(align) * 0.5 + bite * 0.2} r={213} />
      <Motes amount={0.9} t={0.4 + p} />
      <Label text="THE VAULT ANSWERS" o={MOTION.enter(seg(p, 0.2, 0.6))} rise={mix(12, 0, MOTION.enter(seg(p, 0.2, 0.6)))} tone={P.vault} />
    </>
  );
}

function SceneOpen() {
  const { progress: p } = useScene();
  const open = seg(p, 0.1, 1);
  const flash = Math.sin(Math.PI * seg(p, 0.24, 0.84));
  const shock = seg(p, 0.2, 0.9);
  return (
    <>
      <Field dim={mix(0.7, 0.4, MOTION.enter(open))} shake={Math.sin(Math.PI * seg(p, 0.06, 0.5)) * 0.6}
             wash={MOTION.enter(seg(p, 0.84, 1)) * 0.4} />
      <ExtraSlot glow={1 - MOTION.enter(seg(p, 0, 0.5))} count={2} />
      <Vault align={1} open={open} glow={0.9} r={213} />
      {shock > 0 && shock < 1 && (
        <div style={{
          position: 'absolute', left: VAULT.x, top: VAULT.y,
          width: mix(240, 1180, MOTION.enter(shock)), height: mix(240, 1180, MOTION.enter(shock)),
          transform: 'translate(-50%,-50%) rotate(45deg)',
          border: `2px solid ${hexA(P.vault, (1 - shock) * 0.8)}`,
        }} />
      )}
      <div style={{
        position: 'absolute', left: VAULT.x, top: VAULT.y,
        width: 260 + flash * 1700, height: 260 + flash * 1700,
        transform: 'translate(-50%,-50%)', borderRadius: '50%',
        background: `radial-gradient(circle, ${hexA(P.pale, flash * 0.6)}, ${hexA(P.vaultMid, flash * 0.2)} 36%, transparent 62%)`,
      }} />
      <Motes amount={0.8} t={1.3 + p} toward={false} />
    </>
  );
}

function SceneEmerge() {
  const { progress: p } = useScene();
  const rise = MOTION.enter(seg(p, 0.08, 0.82));
  const flip = seg(p, 0.3, 0.9);
  const wash = (1 - MOTION.enter(seg(p, 0, 0.34))) * 0.4;
  return (
    <>
      <Field dim={0.4} />
      <div style={{
        position: 'absolute', left: VAULT.x, top: VAULT.y,
        width: mix(900, 520, rise), height: mix(900, 520, rise),
        transform: 'translate(-50%,-50%)', borderRadius: '50%',
        background: `radial-gradient(circle, ${hexA(P.pale, mix(0.5, 0.16, rise))}, ${hexA(P.vaultMid, mix(0.2, 0.08, rise))} 38%, transparent 64%)`,
      }} />
      {/* a shaft of light the card rides up */}
      <div style={{
        position: 'absolute', left: VAULT.x, top: VAULT.y + 40, width: mix(90, 300, rise), height: 340,
        transform: 'translate(-50%,-100%)',
        background: `linear-gradient(180deg,transparent,${hexA(P.vault, 0.30 * (1 - rise * 0.55))})`,
        clipPath: 'polygon(40% 0,60% 0,100% 100%,0 100%)',
        maskImage: 'linear-gradient(90deg,transparent,#000 34%,#000 66%,transparent)',
        WebkitMaskImage: 'linear-gradient(90deg,transparent,#000 34%,#000 66%,transparent)',
        opacity: 1 - c01(seg(p, 0.7, 1)),
      }} />
      <Card x={VAULT.x} y={mix(VAULT.y + 96, VAULT.y - 4, rise)}
            w={HCW} h={HCH} flip={flip}
            scale={mix(0.4, 1, rise)} glow={mix(0.9, 0.5, rise)} />
      <Motes amount={0.6 * (1 - rise * 0.5)} t={2.1 + p} toward={false} />
      <div style={{ position: 'absolute', inset: 0, background: P.pale, opacity: wash }} />
      <Label text="RELIQUARY SUMMON" o={MOTION.enter(seg(p, 0.52, 0.9))} rise={mix(12, 0, MOTION.enter(seg(p, 0.52, 0.9)))} tone={P.vault} />
    </>
  );
}

function ScenePresent() {
  const { progress: p } = useScene();
  const banner = MOTION.enter(seg(p, 0.02, 0.3));
  const chips = MOTION.enter(seg(p, 0.2, 0.46));
  const settle = cfg.settle ? MOTION.enter(seg(p, 0.6, 0.94)) : 0;
  const out = 1 - MOTION.enter(seg(p, 0.94, 1));
  const breathe = Math.sin(Math.PI * 2 * seg(p, 0.05, 0.6) - Math.PI / 2) * 0.5 + 0.5;
  const hold = 1 - settle;
  return (
    <>
      <Field dim={mix(0.4, 0, settle)} />
      <ExtraSlot glow={0.2 * settle} count={2} caption={0} />
      <div style={{
        position: 'absolute', left: mix(VAULT.x, ZONE.x, settle), top: mix(VAULT.y - 4, ZONE.y, settle),
        width: mix(HCW, FCW, settle) * 2.1, height: mix(HCH, FCH, settle) * 1.5,
        transform: 'translate(-50%,-50%)', borderRadius: '50%',
        background: `radial-gradient(ellipse at 50% 50%, ${hexA(P.vaultMid, (0.16 + breathe * 0.12 * hold) * out)}, transparent 66%)`,
      }} />
      <Card x={mix(VAULT.x, ZONE.x, settle)} y={mix(VAULT.y - 4, ZONE.y, settle)}
            w={HCW} h={HCH} flip={1}
            scale={mix(1, FCW / HCW, settle)}
            glow={(0.42 + breathe * 0.26 * hold) * out} />
      <div style={{
        position: 'absolute', left: 0, right: 0, top: 42,
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 14,
        opacity: banner * hold * out, transform: `translateY(${mix(16, 0, banner)}px)`,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 18 }}>
          <div style={{ width: 88, height: 1, background: `linear-gradient(90deg,transparent,${P.vault})` }} />
          <span style={{ font: `500 13px/1 'Oswald',sans-serif`, letterSpacing: '.42em', color: P.vault, whiteSpace: 'nowrap' }}>RELIQUARY SUMMON</span>
          <div style={{ width: 88, height: 1, background: `linear-gradient(270deg,transparent,${P.vault})` }} />
        </div>
        <span style={{
          font: `700 44px/1.2 'Cinzel',serif`, letterSpacing: '.05em', color: '#F6F1FE',
          whiteSpace: 'nowrap', textShadow: `0 0 44px ${hexA(P.vaultMid, 0.5)}`,
        }}>Special Summon</span>
      </div>
      <div style={{
        position: 'absolute', left: VAULT.x, top: 584, transform: 'translateX(-50%)',
        display: 'flex', gap: 14, opacity: chips * hold * out,
      }}>
        <Chip text="Level 3 · Myth · Light" tone={P.vault} />
        <Chip text="3200 / 2800" tone={P.light} />
        <Chip text="Cannot be targeted on the turn it lands" tone={P.vaultMid} />
      </div>
      <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: MOTION.enter(seg(p, 0.94, 1)) }} />
    </>
  );
}

/* ================= root ================= */
function ExtraSummon() {
  const { useTweaks, TweaksPanel, TweakSection, TweakSlider, TweakToggle } = window;
  const [t, setTweak] = useTweaks(window.TWEAK_DEFAULTS);
  cfg.rings = t.rings;
  cfg.settle = t.settle;
  return (
    <>
      <SceneStage width={W} height={H} scenes={window.OM_SCENES} playback={window.OM_PLAYBACK} bg="#0A0705">
        {{ Call: SceneCall, Unlock: SceneUnlock, Open: SceneOpen, Emerge: SceneEmerge, Present: ScenePresent }}
      </SceneStage>
      <TweaksPanel>
        <TweakSection label="Vault" />
        <TweakSlider label="Ring travel" value={t.rings} min={0} max={2} step={0.05}
                     onChange={(v) => setTweak('rings', v)} />
        <TweakSection label="Ending" />
        <TweakToggle label="Settle into zone" value={t.settle} onChange={(v) => setTweak('settle', v)} />
        <TweakSection label="Editing" />
        <TweakToggle label="Motion editor" value={t.motionEditor} onChange={(v) => setTweak('motionEditor', v)} />
      </TweaksPanel>
    </>
  );
}

window.ExtraSummon = ExtraSummon;
