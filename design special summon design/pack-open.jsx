// pack-open.jsx — RELIQUARY pack opening. The rarity of each card is legible
// from its glow while it is still face-down, so the anticipation sits before
// the flip rather than after it.

const { SceneStage, useScene } = window;

const P = {
  gold: '#C8A45C', light: '#EBCE8A', pale: '#F8EED6', dark: '#7A5A1E', deep: '#3B2A10',
  parch1: '#EBE1C7', parch2: '#D9CCAB', ink: '#2E2417',
  good: '#7ACD96', dim: '#9C8A6A', muted: '#A2917A',
};

const RARITY = {
  common: { name: 'COMMON', c: '#A2917A', deep: '#4A4238', pulse: 0 },
  rare: { name: 'RARE', c: '#8FC6D2', deep: '#1B3A43', pulse: 0.25 },
  epic: { name: 'EPIC', c: '#B9A3E0', deep: '#2A2148', pulse: 0.5 },
  relic: { name: 'RELIC', c: '#EBCE8A', deep: '#3E2C16', pulse: 1 },
};

// five slots, left to right — the relic sits third so the eye lands on it
const PULL = [
  { r: 'common', kind: 'MONSTER', attr: 'EARTH', attrC: '#B98A4E', type: 'ANIMAL', lv: 1, dmg: '900', def: '1200', bar: 0.66 },
  { r: 'rare', kind: 'SPELL', attr: 'WATER', attrC: '#8FC6D2', type: '—', lv: 2, dmg: null, def: null, bar: 0.78 },
  { r: 'relic', kind: 'MONSTER', attr: 'FIRE', attrC: '#E0603A', type: 'DRAGON', lv: 3, dmg: '3000', def: '2400', bar: 0.84 },
  { r: 'epic', kind: 'ARTIFACT', attr: 'DARK', attrC: '#B9A3E0', type: 'MYTH', lv: 2, dmg: null, def: null, bar: 0.72, glossy: true },
  { r: 'common', kind: 'MONSTER', attr: 'WIND', attrC: '#8FD2A8', type: 'HUMAN', lv: 1, dmg: '1400', def: '800', bar: 0.6 },
];

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
const CX = W / 2, CY = 336;
const CW = 176, CH = 246;
const GAP = 22;
const SLOT = PULL.map((_, i) => CX + (i - 2) * (CW + GAP));

const cfg = { glow: 1, glossy: true };

/* ---------------- stage ---------------- */
function Stage({ scale = 1, wash = 0 }) {
  return (
    <div style={{ position: 'absolute', inset: 0, overflow: 'hidden' }}>
      <div style={{
        position: 'absolute', inset: -60, transform: `scale(${scale})`, transformOrigin: '50% 46%',
        background: 'radial-gradient(ellipse 940px 580px at 50% 46%, #2A1C12, #0A0705 78%)',
      }} />
      <div style={{
        position: 'absolute', inset: 0,
        background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.045) 0 1px,transparent 1px 26px),repeating-linear-gradient(-45deg,rgba(200,164,92,.045) 0 1px,transparent 1px 26px)',
      }} />
      <div style={{ position: 'absolute', inset: 0, boxShadow: 'inset 0 0 200px rgba(0,0,0,.88)' }} />
      {wash > 0 && <div style={{ position: 'absolute', inset: 0, background: P.pale, opacity: wash }} />}
    </div>
  );
}

/* ---------------- the sealed pack ---------------- */
function Pack({ x = CX, y = CY, w = 244, h = 342, split = 0, shake = 0, glow = 0, fade = 1 }) {
  if (fade <= 0.001) return null;
  const s = MOTION.enter(split);
  const half = (side) => (
    <div key={side} style={{
      position: 'absolute', left: '50%', top: '50%', width: w * 1.6, height: h * 1.6,
      transform: `translate(calc(-50% + ${side * s * w * 0.85}px),-50%) rotate(${side * s * 13}deg)`,
      clipPath: side < 0 ? 'inset(0 50% 0 0)' : 'inset(0 0 0 50%)',
      opacity: 1 - c01(split / 0.85),
    }}>
      <div style={{
        position: 'absolute', left: '50%', top: '50%', width: w, height: h,
        transform: 'translate(-50%,-50%)', borderRadius: 8, overflow: 'hidden',
        background: 'radial-gradient(ellipse at 50% 40%, #4E2A18, #150C06 80%)',
        border: `2px solid ${P.gold}`, boxShadow: '0 26px 60px rgba(0,0,0,.8)',
      }}>
        <div style={{
          position: 'absolute', inset: 0,
          background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.14) 0 1px,transparent 1px 13px),repeating-linear-gradient(-45deg,rgba(200,164,92,.14) 0 1px,transparent 1px 13px)',
        }} />
        <div style={{ position: 'absolute', inset: 7, border: `1px solid ${hexA(P.gold, 0.45)}`, borderRadius: 4 }} />
        <div style={{
          position: 'absolute', left: '50%', top: '50%', width: 120, height: 120,
          transform: 'translate(-50%,-50%) rotate(45deg)', border: `2px solid ${hexA(P.gold, 0.6)}`,
        }} />
        <div style={{
          position: 'absolute', left: '50%', top: '50%', width: 58, height: 58,
          transform: 'translate(-50%,-50%) rotate(45deg)',
          background: `linear-gradient(135deg,${hexA(P.gold, 0.36)},${hexA(P.gold, 0.05)})`,
          border: `1px solid ${hexA(P.gold, 0.7)}`,
        }} />
        <div style={{
          position: 'absolute', left: '50%', top: '50%', width: 24, height: 24,
          transform: 'translate(-50%,-50%) rotate(45deg)',
          background: `linear-gradient(135deg,${P.light},${P.dark})`,
        }} />
        <div style={{
          position: 'absolute', left: 0, right: 0, bottom: 26, textAlign: 'center',
        }}>
          <span style={{ font: `500 12px/1 'Oswald',sans-serif`, letterSpacing: '.36em', color: P.dim }}>TOMB OF ASH</span>
        </div>
      </div>
    </div>
  );

  return (
    <div style={{
      position: 'absolute', left: x, top: y, width: w, height: h,
      transform: `translate(-50%,-50%) translateX(${Math.sin(shake * Math.PI * 14) * shake * 7}px) rotate(${Math.sin(shake * Math.PI * 9) * shake * 2}deg)`,
      opacity: fade,
      filter: `drop-shadow(0 0 ${14 + glow * 54}px ${hexA(P.light, 0.2 + glow * 0.5)})`,
    }}>
      {half(-1)}
      {half(1)}
    </div>
  );
}

/* ---------------- card faces ---------------- */
function Back() {
  return (
    <div style={{
      position: 'absolute', inset: 0, borderRadius: 7, overflow: 'hidden',
      background: 'radial-gradient(ellipse at 50% 50%, #4E2A18, #1C0E08 78%)',
      border: `2px solid ${P.gold}`, backfaceVisibility: 'hidden',
    }}>
      <div style={{
        position: 'absolute', inset: 0,
        background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.13) 0 1px,transparent 1px 11px),repeating-linear-gradient(-45deg,rgba(200,164,92,.13) 0 1px,transparent 1px 11px)',
      }} />
      <div style={{ position: 'absolute', inset: 5, border: `1px solid ${hexA(P.gold, 0.5)}`, borderRadius: 3 }} />
      <div style={{
        position: 'absolute', left: '50%', top: '50%', width: 84, height: 84,
        transform: 'translate(-50%,-50%) rotate(45deg)', border: `2px solid ${hexA(P.gold, 0.6)}`,
      }} />
      <div style={{
        position: 'absolute', left: '50%', top: '50%', width: 84, height: 84,
        transform: 'translate(-50%,-50%)', border: `1px solid ${hexA(P.gold, 0.3)}`,
      }} />
      <div style={{
        position: 'absolute', left: '50%', top: '50%', width: 42, height: 42,
        transform: 'translate(-50%,-50%) rotate(45deg)',
        background: `linear-gradient(135deg,${hexA(P.gold, 0.35)},${hexA(P.gold, 0.05)})`,
        border: `1px solid ${hexA(P.gold, 0.7)}`,
      }} />
      <div style={{
        position: 'absolute', left: '50%', top: '50%', width: 17, height: 17,
        transform: 'translate(-50%,-50%) rotate(45deg)',
        background: `linear-gradient(135deg,${P.light},${P.dark})`,
      }} />
    </div>
  );
}

function Front({ d, sweep = -1 }) {
  const R = RARITY[d.r];
  return (
    <div style={{
      position: 'absolute', inset: 0, borderRadius: 7, overflow: 'hidden',
      boxSizing: 'border-box', padding: '7px 15px',
      background: 'linear-gradient(165deg,#332315,#150d07 55%,#251809)',
      border: `2px solid ${R.c}`, backfaceVisibility: 'hidden',
      transform: 'rotateY(180deg)',
    }}>
      <div style={{ position: 'absolute', inset: 4, border: `1px solid ${hexA(R.c, 0.4)}`, borderRadius: 4 }} />
      {[['left', 'top'], ['right', 'top'], ['left', 'bottom'], ['right', 'bottom']].map(([a, b]) => (
        <div key={a + b} style={{ position: 'absolute', [a]: 8, [b]: 8, width: 8, height: 8, transform: 'rotate(45deg)', background: R.c }} />
      ))}
      <div style={{ position: 'relative', width: 146, display: 'flex', flexDirection: 'column', gap: 3 }}>
        <div style={{ height: 20, display: 'flex', alignItems: 'center', gap: 4 }}>
          <div style={{
            flex: 1, height: 20, display: 'flex', alignItems: 'center', padding: '0 8px', boxSizing: 'border-box',
            background: 'linear-gradient(180deg,#42301C,#22150A)',
            borderTop: `1px solid ${P.gold}`, borderBottom: `1px solid ${P.gold}`,
            clipPath: 'polygon(0 0,100% 0,calc(100% - 7px) 100%,7px 100%)', overflow: 'hidden',
          }}>
            <div style={{ width: `${d.bar * 100}%`, height: 4, background: `linear-gradient(90deg,${hexA(R.c, 0.85)},${hexA(R.c, 0.25)})` }} />
          </div>
          <div style={{
            position: 'relative', width: 18, height: 20, flex: 'none',
            background: `linear-gradient(160deg,${P.light},#8E6A22)`,
            clipPath: 'polygon(50% 0,100% 20%,100% 66%,50% 100%,0 66%,0 20%)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
          }}>
            <div style={{
              position: 'absolute', inset: 1.5, background: 'linear-gradient(160deg,#3B2A10,#180F04)',
              clipPath: 'polygon(50% 0,100% 20%,100% 66%,50% 100%,0 66%,0 20%)',
            }} />
            <span style={{ position: 'relative', font: `700 10px/1.2 'Cinzel',serif`, color: '#F3DDA4' }}>{d.lv}</span>
          </div>
        </div>
        <div style={{
          width: 132, height: 132, alignSelf: 'center', boxSizing: 'border-box', padding: 4,
          background: 'linear-gradient(160deg,#3E2C16,#1A1108)', border: `2px solid ${P.gold}`,
        }}>
          <div style={{ width: '100%', height: '100%', boxSizing: 'border-box', overflow: 'hidden', border: `1px solid ${hexA(P.gold, 0.65)}` }}>
            <img src="uploads/artwork-1785612438938.png" alt="" style={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }} />
          </div>
        </div>
        <div style={{ height: 13, display: 'flex', gap: 2 }}>
          <div style={{
            flex: 'none', padding: '0 5px', display: 'flex', alignItems: 'center',
            background: 'linear-gradient(180deg,#E2C685,#9C7526)', color: '#1E1405',
            font: `600 6px/1 'Oswald',sans-serif`, letterSpacing: '.1em',
          }}>{d.kind}</div>
          <div style={{
            flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '0 5px',
            background: 'rgba(0,0,0,.35)', border: `1px solid ${hexA(P.gold, 0.45)}`,
          }}>
            <span style={{ display: 'flex', alignItems: 'center', gap: 3, font: `500 6px/1 'Oswald',sans-serif`, letterSpacing: '.1em', color: '#E4D3AE' }}>
              <i style={{ width: 5, height: 5, display: 'block', background: d.attrC, transform: 'rotate(45deg)' }} />{d.attr}
            </span>
            <span style={{ font: `500 6px/1 'Oswald',sans-serif`, letterSpacing: '.1em', color: '#B5A484' }}>{d.type}</span>
          </div>
        </div>
        <div style={{
          height: d.dmg ? 40 : 58, boxSizing: 'border-box', padding: '5px 6px',
          background: `linear-gradient(180deg,${P.parch1},${P.parch2})`, border: '1px solid #8C7440',
          display: 'flex', flexDirection: 'column', gap: 3,
        }}>
          <div style={{ width: '95%', height: 3, background: 'rgba(46,36,23,.32)' }} />
          <div style={{ width: '88%', height: 3, background: 'rgba(46,36,23,.32)' }} />
          <div style={{ width: '92%', height: 3, background: 'rgba(46,36,23,.32)' }} />
          {!d.dmg && <div style={{ width: '70%', height: 3, background: 'rgba(46,36,23,.32)' }} />}
          <div style={{ width: '58%', height: 3, background: 'rgba(46,36,23,.32)' }} />
        </div>
        {d.dmg && (
          <div style={{ height: 17, display: 'flex', gap: 3 }}>
            <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 4, background: 'rgba(0,0,0,.4)', border: '1px solid rgba(224,96,58,.5)' }}>
              <span style={{ font: `500 6px/1 'Oswald',sans-serif`, letterSpacing: '.12em', color: '#C08D74' }}>DMG</span>
              <span style={{ font: `700 9px/1.2 'Cinzel',serif`, color: '#F3C3A6' }}>{d.dmg}</span>
            </div>
            <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 4, background: 'rgba(0,0,0,.4)', border: '1px solid rgba(143,198,210,.5)' }}>
              <span style={{ font: `500 6px/1 'Oswald',sans-serif`, letterSpacing: '.12em', color: '#7FAAB4' }}>DEF</span>
              <span style={{ font: `700 9px/1.2 'Cinzel',serif`, color: '#B9E6F0' }}>{d.def}</span>
            </div>
          </div>
        )}
      </div>
      {sweep >= 0 && (
        <div style={{ position: 'absolute', inset: 0, overflow: 'hidden', pointerEvents: 'none' }}>
          <div style={{
            position: 'absolute', top: '-30%', left: 0, width: '46%', height: '160%',
            transform: `rotate(18deg) translateX(${mix(-170, 260, sweep)}%)`,
            background: 'linear-gradient(90deg,transparent,rgba(255,255,255,.5) 46%,rgba(255,255,255,.72) 52%,transparent)',
            mixBlendMode: 'screen',
          }} />
        </div>
      )}
    </div>
  );
}

/* ---------------- rarity flames ----------------
   Tongues rooted at the card's lower edge, tallest in the middle, each flickering
   on its own fixed phase so the field is frame-deterministic and still exports.
   Count and height climb with rarity — a relic burns past the top of the card. */
const FLAME_N = { common: 5, rare: 7, epic: 9, relic: 13 };

function Flames({ x, y, r, amount, t }) {
  if (amount <= 0.01) return null;
  const R = RARITY[r];
  const n = FLAME_N[r];
  const spread = CW * 1.24;
  const baseH = CH * mix(0.52, 1.06, R.pulse);
  const rootY = y + CH / 2 - 8;

  return (
    <div style={{ position: 'absolute', left: x, top: rootY, width: 0, height: 0 }}>
      <div style={{
        position: 'absolute', left: -spread * 0.5, top: -26, width: spread, height: 52,
        borderRadius: '50%',
        background: `radial-gradient(ellipse at 50% 50%, ${hexA(R.c, 0.5 * amount)}, transparent 70%)`,
        filter: 'blur(6px)',
      }} />
      {Array.from({ length: n }, (_, i) => {
        const u = n === 1 ? 0.5 : i / (n - 1);
        const dx = (u - 0.5) * spread;
        const centre = Math.max(0.2, 1 - Math.abs(u - 0.5) * 1.55);
        const flick = 0.6 + 0.4 * Math.sin(Math.PI * 2 * (t * 1.7 + i * 0.29));
        const h = baseH * centre * flick * amount;
        const w = mix(15, 31, centre) * mix(0.82, 1.16, flick);
        const sway = Math.sin(Math.PI * 2 * (t * 1.1 + i * 0.41)) * 6;
        return (
          <div key={i} style={{
            position: 'absolute', left: dx - w / 2, top: -h, width: w, height: h,
            transform: `skewX(${sway}deg)`,
            clipPath: 'polygon(50% 0,82% 34%,100% 72%,74% 100%,26% 100%,0 72%,18% 34%)',
            background: `linear-gradient(180deg,transparent,${hexA(R.c, 0.16)} 20%,${hexA(R.c, 0.58)} 60%,${hexA(R.c, 0.92)})`,
            filter: `blur(${mix(1.5, 4.5, 1 - centre)}px)`,
            opacity: 0.9 * amount,
          }}>
            <div style={{
              position: 'absolute', left: '28%', bottom: 0, width: '44%', height: '52%',
              clipPath: 'polygon(50% 0,100% 60%,72% 100%,28% 100%,0 60%)',
              background: `linear-gradient(180deg,transparent,${hexA(P.pale, 0.5 * R.pulse + 0.18)})`,
            }} />
          </div>
        );
      })}
      {R.pulse >= 0.5 && Array.from({ length: 5 }, (_, i) => {
        const ph = (t * 0.85 + i * 0.21) % 1;
        const s = 4 + (i % 3);
        return (
          <div key={'e' + i} style={{
            position: 'absolute',
            left: (i / 4 - 0.5) * spread * 0.72 + Math.sin(Math.PI * 2 * (ph + i)) * 12,
            top: -baseH * (0.55 + ph * 1.05), width: s, height: s,
            transform: 'rotate(45deg)', background: R.c,
            opacity: Math.sin(Math.PI * ph) * 0.75 * amount,
          }} />
        );
      })}
    </div>
  );
}

/* ---------------- one card in flight ----------------
   `flip` 0 = back to camera, 1 = front. The rarity glow lives behind the card
   and is at full strength while the card is still face-down. */
function Card({ d, x, y, rot = 0, scale = 1, flip = 0, glow = 1, lift = 0, o = 1, sweep = -1, t = 0 }) {
  if (o <= 0.001) return null;
  const R = RARITY[d.r];
  const a = MOTION.drift(flip) * 180;
  const g = glow * cfg.glow;
  return (
    <>
      {g > 0.002 && (
        <>
          <div style={{
            position: 'absolute', left: x, top: y - lift,
            width: CW * mix(1.5, 2.1, R.pulse), height: CH * mix(1.25, 1.6, R.pulse),
            transform: 'translate(-50%,-50%)', borderRadius: '50%',
            background: `radial-gradient(ellipse at 50% 50%, ${hexA(R.c, 0.26 * g)}, ${hexA(R.c, 0.08 * g)} 44%, transparent 68%)`,
          }} />
          <Flames x={x} y={y - lift} r={d.r} amount={g} t={t} />
        </>
      )}
      <div style={{
        position: 'absolute', left: x, top: y - lift, width: CW, height: CH,
        transform: `translate(-50%,-50%) rotate(${rot}deg) scale(${scale})`,
        perspective: 1400, opacity: o,
        filter: `drop-shadow(0 ${12 + lift * 0.4}px ${22 + lift * 0.5}px rgba(0,0,0,.72))`,
      }}>
        <div style={{
          position: 'relative', width: '100%', height: '100%',
          transformStyle: 'preserve-3d', transform: `rotateY(${a}deg)`,
        }}>
          <Back />
          <Front d={d} sweep={sweep} />
        </div>
      </div>
    </>
  );
}

/* ---------------- furniture ---------------- */
function Eyebrow({ text, o, rise, tone }) {
  if (o <= 0.001) return null;
  return (
    <div style={{
      position: 'absolute', left: 0, right: 0, top: 66,
      display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 18,
      opacity: o, transform: `translateY(${rise}px)`,
    }}>
      <div style={{ width: 76, height: 1, background: `linear-gradient(90deg,transparent,${tone})` }} />
      <span style={{ font: `500 14px/1 'Oswald',sans-serif`, letterSpacing: '.4em', color: tone, whiteSpace: 'nowrap' }}>{text}</span>
      <div style={{ width: 76, height: 1, background: `linear-gradient(270deg,transparent,${tone})` }} />
    </div>
  );
}

function Chip({ text, tone }) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 10, padding: '11px 18px',
      background: 'rgba(0,0,0,.5)', border: `1px solid ${hexA(tone, 0.5)}`,
    }}>
      <i style={{ width: 8, height: 8, display: 'block', background: tone, transform: 'rotate(45deg)' }} />
      <span style={{ font: `400 15px/1 'Spectral',serif`, color: '#C8B189', whiteSpace: 'nowrap' }}>{text}</span>
    </div>
  );
}

/* labels under the face-down cards — the rarity is named before it is shown */
function Tags({ o, glow }) {
  if (o <= 0.001) return null;
  return PULL.map((d, i) => {
    const R = RARITY[d.r];
    return (
      <div key={i} style={{
        position: 'absolute', left: SLOT[i], top: CY + CH / 2 + 30,
        transform: 'translateX(-50%)', opacity: o * mix(0.5, 1, R.pulse * glow),
      }}>
        <span style={{ font: `500 11px/1 'Oswald',sans-serif`, letterSpacing: '.24em', color: R.c, whiteSpace: 'nowrap' }}>{R.name}</span>
      </div>
    );
  });
}

/* ================= SCENES =================
   ┌────────┬───────────────┬──────────────┬────────────┐
   │ scene  │ pack split    │ card spread  │ flip       │
   ├────────┼───────────────┼──────────────┼────────────┤
   │ Seal   │ 0             │ stacked      │ 0          │
   │ Tear   │ 0 → 1         │ stacked      │ 0          │
   │ Fan    │ gone          │ 0 → 1        │ 0          │
   │ Flip   │ gone          │ 1            │ 0 → 1      │
   │ Hold   │ gone          │ 1            │ 1          │
   └────────┴───────────────┴──────────────┴────────────┘ */

function SceneSeal() {
  const { progress: p } = useScene();
  const inn = MOTION.enter(seg(p, 0.04, 0.36));
  const tension = seg(p, 0.3, 1);
  return (
    <>
      <Stage scale={mix(1.02, 1.06, MOTION.drift(p))} />
      <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: 1 - MOTION.enter(seg(p, 0, 0.2)) }} />
      <Pack shake={tension * 0.5} glow={0.1 + tension * 0.35} fade={inn} />
      <Eyebrow text="SEALED PACK" o={inn} rise={mix(14, 0, inn)} tone={P.dim} />
      <div style={{
        position: 'absolute', left: 0, right: 0, top: 108, textAlign: 'center', opacity: inn,
        transform: `translateY(${mix(16, 0, inn)}px)`,
      }}>
        <span style={{ font: `700 44px/1.2 'Cinzel',serif`, letterSpacing: '.06em', color: '#F1DFB8' }}>Tomb of Ash</span>
      </div>
      <div style={{
        position: 'absolute', left: 0, right: 0, bottom: 74, textAlign: 'center',
        opacity: inn * (0.5 + Math.sin(Math.PI * 2 * tension) * 0.3 + 0.2),
      }}>
        <span style={{ font: `400 17px/1 'Spectral',serif`, color: P.muted }}>Five cards. One guaranteed rare or better.</span>
      </div>
    </>
  );
}

function SceneTear() {
  const { progress: p } = useScene();
  const split = seg(p, 0.12, 1);
  const flash = Math.sin(Math.PI * seg(p, 0.1, 0.7));
  const shock = seg(p, 0.14, 0.8);
  return (
    <>
      <Stage scale={mix(1.06, 1.16, MOTION.drift(p))} wash={MOTION.enter(seg(p, 0.84, 1)) * 0.42} />
      <Pack split={split} shake={0.5 * (1 - MOTION.enter(split))} glow={mix(0.45, 1, MOTION.enter(seg(p, 0, 0.5)))} />
      {shock > 0 && shock < 1 && (
        <div style={{
          position: 'absolute', left: CX, top: CY,
          width: mix(200, 980, MOTION.enter(shock)), height: mix(200, 980, MOTION.enter(shock)),
          transform: 'translate(-50%,-50%) rotate(45deg)',
          border: `2px solid ${hexA(P.pale, (1 - shock) * 0.72)}`,
        }} />
      )}
      <div style={{
        position: 'absolute', left: CX, top: CY,
        width: 220 + flash * 1800, height: 220 + flash * 1800,
        transform: 'translate(-50%,-50%)', borderRadius: '50%',
        background: `radial-gradient(circle, ${hexA(P.pale, flash * 0.55)}, ${hexA(P.light, flash * 0.18)} 36%, transparent 62%)`,
      }} />
    </>
  );
}

function SceneFan() {
  const { progress: p } = useScene();
  const wash = (1 - MOTION.enter(seg(p, 0, 0.3))) * 0.42;
  const tags = MOTION.enter(seg(p, 0.62, 1));
  return (
    <>
      <Stage scale={mix(1.16, 1.02, MOTION.drift(p))} wash={wash} />
      {PULL.map((d, i) => {
        // each card leaves the burst on its own beat, then eases into its slot
        const t = MOTION.enter(seg(p, 0.04 + i * 0.075, 0.62 + i * 0.075));
        return (
          <Card key={i} d={d}
            x={mix(CX, SLOT[i], t)}
            y={mix(CY - 30, CY, t)}
            rot={mix((i - 2) * 22, 0, t)}
            scale={mix(0.62, 1, t)}
            glow={t}
            t={p}
            o={t > 0.001 ? 1 : 0} />
        );
      })}
      <Tags o={tags} glow={tags} />
      <Eyebrow text="FIVE CARDS" o={MOTION.enter(seg(p, 0.5, 0.86))} rise={mix(12, 0, MOTION.enter(seg(p, 0.5, 0.86)))} tone={P.dim} />
    </>
  );
}

function SceneFlip() {
  const { progress: p } = useScene();
  return (
    <>
      <Stage scale={mix(1.02, 1.04, MOTION.drift(p))} />
      {PULL.map((d, i) => {
        const f = seg(p, 0.06 + i * 0.09, 0.5 + i * 0.09);
        const R = RARITY[d.r];
        // the glow stays up until the card is past its edge, then hands over to the frame
        const glow = (1 - c01(f / 0.55)) * mix(0.7, 1, R.pulse);
        const kick = Math.sin(Math.PI * seg(f, 0, 1));
        return (
          <Card key={i} d={d} x={SLOT[i]} y={CY} flip={f}
            scale={1 + kick * 0.08 * mix(0.5, 1.4, R.pulse)}
            lift={kick * mix(10, 26, R.pulse)}
            glow={glow} t={p} />
        );
      })}
      <Tags o={1 - MOTION.enter(seg(p, 0.1, 0.5))} glow={1 - MOTION.enter(seg(p, 0.1, 0.5))} />
      <Eyebrow text="FIVE CARDS" o={1 - MOTION.enter(seg(p, 0, 0.3))} rise={mix(0, -10, MOTION.enter(seg(p, 0, 0.3)))} tone={P.dim} />
    </>
  );
}

function SceneHold() {
  const { progress: p } = useScene();
  const inn = MOTION.enter(seg(p, 0.06, 0.36));
  const chips = MOTION.enter(seg(p, 0.34, 0.6));
  const out = 1 - MOTION.enter(seg(p, 0.94, 1));
  const breathe = Math.sin(Math.PI * 2 * seg(p, 0.1, 1) - Math.PI / 2) * 0.5 + 0.5;
  return (
    <>
      <Stage scale={mix(1.04, 1.02, MOTION.drift(p))} />
      {PULL.map((d, i) => {
        const R = RARITY[d.r];
        const hero = d.r === 'relic';
        return (
          <Card key={i} d={d} x={SLOT[i]} y={CY} flip={1}
            lift={hero ? mix(0, 22, inn) + breathe * 5 : 0}
            scale={hero ? mix(1, 1.07, inn) : 1}
            glow={hero ? (0.34 + breathe * 0.26) * out : 0.1 * out}
            t={p}
            sweep={cfg.glossy && d.glossy ? (p * 1.6) % 1 : -1} />
        );
      })}
      <div style={{
        position: 'absolute', left: SLOT[2], top: CY - CH / 2 - 60,
        transform: 'translateX(-50%)', opacity: inn * out,
      }}>
        <div style={{
          display: 'flex', alignItems: 'center', gap: 11, padding: '9px 18px',
          background: 'rgba(0,0,0,.55)', border: `1px solid ${hexA(RARITY.relic.c, 0.75)}`,
          boxShadow: `0 0 ${20 + breathe * 18}px ${hexA(RARITY.relic.c, 0.3)}`,
        }}>
          <i style={{ width: 9, height: 9, display: 'block', background: RARITY.relic.c, transform: 'rotate(45deg)' }} />
          <span style={{ font: `600 16px/1.2 'Cinzel',serif`, letterSpacing: '.14em', color: '#F8EED6', whiteSpace: 'nowrap' }}>RELIC · NEW</span>
        </div>
      </div>
      <Eyebrow text="PACK OPENED" o={inn * out} rise={mix(12, 0, inn)} tone={P.dim} />
      <div style={{
        position: 'absolute', left: CX, top: CY + CH / 2 + 52, transform: 'translateX(-50%)',
        display: 'flex', gap: 14, opacity: chips * out,
      }}>
        <Chip text="1 new card" tone={P.good} />
        <Chip text="4 duplicates stored" tone={P.light} />
        <Chip text="1 Glossy pull" tone={RARITY.epic.c} />
      </div>
      <div style={{
        position: 'absolute', left: CX, top: CY + CH / 2 + 108, transform: 'translateX(-50%)',
        opacity: chips * out * 0.9,
      }}>
        <span style={{ font: `400 15px/1 'Spectral',serif`, color: P.dim, whiteSpace: 'nowrap' }}>
          Turn duplicates into crafting material in the Deck Builder.
        </span>
      </div>
      <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: MOTION.enter(seg(p, 0.94, 1)) }} />
    </>
  );
}

/* ================= root ================= */
function PackOpen() {
  const { useTweaks, TweaksPanel, TweakSection, TweakSlider, TweakToggle } = window;
  const [t, setTweak] = useTweaks(window.TWEAK_DEFAULTS);
  cfg.glow = t.glow;
  cfg.glossy = t.glossy;
  return (
    <>
      <SceneStage width={W} height={H} scenes={window.OM_SCENES} playback={window.OM_PLAYBACK} bg="#0A0705">
        {{ Seal: SceneSeal, Tear: SceneTear, Fan: SceneFan, Flip: SceneFlip, Hold: SceneHold }}
      </SceneStage>
      <TweaksPanel>
        <TweakSection label="Rarity tell" />
        <TweakSlider label="Glow strength" value={t.glow} min={0} max={1.6} step={0.05}
                     onChange={(v) => setTweak('glow', v)} />
        <TweakToggle label="Glossy pull" value={t.glossy} onChange={(v) => setTweak('glossy', v)} />
        <TweakSection label="Editing" />
        <TweakToggle label="Motion editor" value={t.motionEditor} onChange={(v) => setTweak('motionEditor', v)} />
      </TweaksPanel>
    </>
  );
}

window.PackOpen = PackOpen;
