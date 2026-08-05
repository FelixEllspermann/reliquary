// player-defeat.jsx — RELIQUARY: the player card takes the final direct attack.
// The player is a card too, so losing is the same grammar as any other
// destruction — struck, cracked, shattered — only slower, and the board goes
// with it.

const { SceneStage, useScene } = window;

const P = {
  gold: '#C8A45C', light: '#EBCE8A', pale: '#F8EED6', dark: '#7A5A1E',
  parch1: '#EBE1C7', parch2: '#D9CCAB', ink: '#2E2417',
  ember: '#E0603A', emberLit: '#F3C3A6', emberDark: '#8A3418',
  teal: '#8FC6D2', violet: '#B9A3E0', ash: '#8A857B', ashDark: '#4A4640',
  good: '#7ACD96', dim: '#9C8A6A', muted: '#A2917A',
};

const c01 = (v) => (v < 0 ? 0 : v > 1 ? 1 : v);
const seg = (p, a, b) => c01((p - a) / (b - a));
const mix = (a, b, t) => a + (b - a) * t;
const num = (v) => Math.round(v).toLocaleString('en-US').replace(/,/g, ' ');
const hexA = (hex, a) => {
  const h = hex.replace('#', '');
  return `rgba(${parseInt(h.slice(0, 2), 16)},${parseInt(h.slice(2, 4), 16)},${parseInt(h.slice(4, 6), 16)},${a})`;
};
const mixHex = (a, b, t) => {
  const pa = a.replace('#', ''), pb = b.replace('#', '');
  const ch = (s, i) => parseInt(s.slice(i, i + 2), 16);
  return `rgb(${Math.round(mix(ch(pa, 0), ch(pb, 0), t))},${Math.round(mix(ch(pa, 2), ch(pb, 2), t))},${Math.round(mix(ch(pa, 4), ch(pb, 4), t))})`;
};

const MOTION = {
  enter: (p) => 1 - Math.pow(1 - c01(p), 3),
  drift: (p) => 0.5 - 0.5 * Math.cos(Math.PI * c01(p)),
  pop: (p) => { const t = c01(p), s = 1.9; return 1 + (s + 1) * Math.pow(t - 1, 3) + s * Math.pow(t - 1, 2); },
};

const W = 1280, H = 720;
const PC = { x: 640, y: 476, w: 200, h: 280 };   // the player card — low enough to leave the attacker a visible lane
const FOE = { x: 640, y: 181, w: 120, h: 168 };  // the attacker — sits between the label band (ends y 82) and the centre line (y 268)
const LP_MAX = 8000, LP_START = 400, HIT = 2600;
// The attacker renders at scale(1.14) at full lunge, so the stop is derived from
// its SCALED height and lands 6px above the player card's top edge — the name
// plate (the thing that makes it a player card) must stay readable through the blow.
const FOE_H_LUNGED = FOE.h * 1.14;
const LUNGE_Y = PC.y - PC.h / 2 - FOE_H_LUNGED / 2 - 6;

const cfg = { drain: 1, aftermath: true };

/* ---------------- field ---------------- */
function Field({ dim = 0, shake = 0, ash = 0 }) {
  const sx = Math.sin(shake * Math.PI * 13) * shake * 9;
  const sy = Math.cos(shake * Math.PI * 9) * shake * 6;
  return (
    <div style={{
      position: 'absolute', inset: 0, overflow: 'hidden',
      transform: `translate(${sx}px,${sy}px)`,
      filter: ash > 0.01 ? `saturate(${1 - ash * 0.9}) brightness(${1 - ash * 0.42})` : 'none',
    }}>
      <div style={{ position: 'absolute', inset: -40, background: 'radial-gradient(ellipse 980px 620px at 50% 46%, #2A1C12, #0A0705 78%)' }} />
      <div style={{
        position: 'absolute', inset: 0,
        background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.04) 0 1px,transparent 1px 26px),repeating-linear-gradient(-45deg,rgba(200,164,92,.04) 0 1px,transparent 1px 26px)',
      }} />
      <div style={{ position: 'absolute', left: 0, right: 0, top: 268, height: 1, background: hexA(P.gold, 0.2) }} />
      <div style={{
        position: 'absolute', left: W / 2, top: 268, width: 20, height: 20,
        transform: 'translate(-50%,-50%) rotate(45deg)', background: '#0A0705', border: `1px solid ${hexA(P.gold, 0.38)}`,
      }} />
      <div style={{
        position: 'absolute', left: PC.x, top: PC.y, width: 620, height: 620,
        transform: 'translate(-50%,-50%) rotate(45deg)', border: `1px solid ${hexA(P.gold, 0.07)}`,
      }} />
      <div style={{ position: 'absolute', inset: 0, boxShadow: 'inset 0 0 190px rgba(0,0,0,.9)' }} />
      {dim > 0 && <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: dim * 0.6 }} />}
    </div>
  );
}

/* ---------------- the attacking monster ---------------- */
function Attacker({ y, lunge = 0, glow = 0 }) {
  const w = FOE.w, h = FOE.h;
  return (
    <div style={{
      position: 'absolute', left: FOE.x, top: y,
      transform: `translate(-50%,-50%) scale(${1 + lunge * 0.14})`,
      filter: `drop-shadow(0 14px 30px rgba(0,0,0,.8)) drop-shadow(0 0 ${12 + glow * 40}px ${hexA(P.violet, 0.25 + glow * 0.5)})`,
    }}>
      <div style={{
        position: 'relative', width: w, height: h, borderRadius: 5, overflow: 'hidden',
        boxSizing: 'border-box', padding: `${h * 0.03}px ${w * 0.1}px`,
        background: 'linear-gradient(165deg,#332315,#150d07 55%,#251809)', border: `2px solid ${P.violet}`,
      }}>
        <div style={{ position: 'absolute', inset: 3, border: `1px solid ${hexA(P.violet, 0.4)}`, borderRadius: 3 }} />
        <div style={{ position: 'relative', width: w - w * 0.2, display: 'flex', flexDirection: 'column', gap: 3 }}>
          <div style={{
            height: 15, display: 'flex', alignItems: 'center', padding: '0 6px', boxSizing: 'border-box',
            background: 'linear-gradient(180deg,#42301C,#22150A)',
            borderTop: `1px solid ${P.gold}`, borderBottom: `1px solid ${P.gold}`,
            clipPath: 'polygon(0 0,100% 0,calc(100% - 6px) 100%,6px 100%)', overflow: 'hidden',
          }}>
            <div style={{ width: '70%', height: 3, background: `linear-gradient(90deg,${hexA(P.violet, 0.85)},${hexA(P.violet, 0.25)})` }} />
          </div>
          <div style={{
            width: '92%', alignSelf: 'center', aspectRatio: '1', boxSizing: 'border-box', padding: 3,
            background: 'linear-gradient(160deg,#3E2C16,#1A1108)', border: `2px solid ${P.gold}`,
          }}>
            <div style={{ width: '100%', height: '100%', boxSizing: 'border-box', overflow: 'hidden', border: `1px solid ${hexA(P.gold, 0.65)}` }}>
              <img src="uploads/artwork-1785612438938.png" alt="" style={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }} />
            </div>
          </div>
          <div style={{
            height: 14, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 5,
            background: 'rgba(0,0,0,.45)', border: `1px solid ${hexA(P.ember, 0.55)}`,
          }}>
            <span style={{ font: `500 6px/1 'Oswald',sans-serif`, letterSpacing: '.14em', color: '#C08D74' }}>DMG</span>
            <span style={{ font: `700 11px/1.2 'Cinzel',serif`, color: P.emberLit }}>{num(HIT)}</span>
          </div>
        </div>
      </div>
    </div>
  );
}

/* ---------------- the player card ----------------
   Same frame language as a monster, but the stat block is LIFE. The frame and
   the LP block go ember as the total falls, so the state is legible from across
   the table without reading the number. */
function PlayerCard({ lp, drain = 0, hurt = 0, w = PC.w, h = PC.h }) {
  const frac = c01(lp / LP_MAX);
  const critical = 1 - c01(lp / 1200);
  const edge = mixHex(mixHex(P.gold, P.ember, critical * 0.85), P.ash, drain);
  const lpCol = lp <= 0 ? P.ash : mixHex(P.pale, P.emberLit, critical);
  return (
    <div style={{
      position: 'relative', width: w, height: h, borderRadius: 7, overflow: 'hidden',
      boxSizing: 'border-box', padding: `${h * 0.032}px ${w * 0.075}px`,
      background: 'linear-gradient(165deg,#3A2818,#150d07 58%,#291A0C)',
      border: `3px solid ${edge}`,
      filter: drain > 0.01 ? `saturate(${1 - drain * 0.9}) brightness(${1 - drain * 0.3})` : 'none',
    }}>
      <div style={{ position: 'absolute', inset: 5, border: `1px solid ${hexA(edge, 0.4)}`, borderRadius: 4 }} />
      {[['left', 'top'], ['right', 'top'], ['left', 'bottom'], ['right', 'bottom']].map(([a, b]) => (
        <div key={a + b} style={{ position: 'absolute', [a]: 10, [b]: 10, width: 9, height: 9, transform: 'rotate(45deg)', background: edge }} />
      ))}
      {/* height:100% makes the column a definite-height flex container, so the
          artwork below can absorb the leftover space instead of overflowing */}
      <div style={{ position: 'relative', width: w - w * 0.15, height: '100%', display: 'flex', flexDirection: 'column', gap: h * 0.018 }}>
        <div style={{ height: h * 0.088, flex: 'none', display: 'flex', alignItems: 'center', gap: 5 }}>
          <div style={{
            flex: 1, height: '100%', display: 'flex', alignItems: 'center', padding: '0 9px', boxSizing: 'border-box',
            background: 'linear-gradient(180deg,#42301C,#22150A)',
            borderTop: `1px solid ${P.gold}`, borderBottom: `1px solid ${P.gold}`,
            clipPath: 'polygon(0 0,100% 0,calc(100% - 8px) 100%,8px 100%)', overflow: 'hidden',
          }}>
            <span style={{ font: `600 ${h * 0.05}px/1.25 'Cinzel',serif`, color: '#F1DFB8', whiteSpace: 'nowrap' }}>DiaPony</span>
          </div>
          <div style={{
            position: 'relative', width: h * 0.082, height: h * 0.09, flex: 'none',
            background: `linear-gradient(160deg,${P.light},#8E6A22)`,
            clipPath: 'polygon(50% 0,100% 20%,100% 66%,50% 100%,0 66%,0 20%)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
          }}>
            <div style={{ position: 'absolute', inset: 2, background: 'linear-gradient(160deg,#3B2A10,#180F04)', clipPath: 'polygon(50% 0,100% 20%,100% 66%,50% 100%,0 66%,0 20%)' }} />
            <span style={{ position: 'relative', font: `700 ${h * 0.044}px/1.2 'Cinzel',serif`, color: '#F3DDA4' }}>27</span>
          </div>
        </div>
        <div style={{
          flex: '1 1 auto', minHeight: 0, aspectRatio: '1', alignSelf: 'center',
          boxSizing: 'border-box', padding: 5,
          background: 'linear-gradient(160deg,#3E2C16,#1A1108)', border: `2px solid ${P.gold}`,
        }}>
          <div style={{ width: '100%', height: '100%', boxSizing: 'border-box', overflow: 'hidden', border: `1px solid ${hexA(P.gold, 0.65)}` }}>
            <img src="uploads/artwork-1785612438938.png" alt="" style={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }} />
          </div>
        </div>
        <div style={{ height: h * 0.052, flex: 'none', display: 'flex', gap: 3 }}>
          <div style={{
            flex: 'none', padding: '0 6px', display: 'flex', alignItems: 'center',
            background: 'linear-gradient(180deg,#E2C685,#9C7526)', color: '#1E1405',
            font: `600 ${h * 0.026}px/1 'Oswald',sans-serif`, letterSpacing: '.12em',
          }}>DUELIST</div>
          <div style={{
            flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '0 6px',
            background: 'rgba(0,0,0,.35)', border: `1px solid ${hexA(P.gold, 0.45)}`,
          }}>
            <span style={{ display: 'flex', alignItems: 'center', gap: 4, font: `500 ${h * 0.026}px/1 'Oswald',sans-serif`, letterSpacing: '.12em', color: '#E4D3AE' }}>
              <i style={{ width: 5, height: 5, display: 'block', background: P.light, transform: 'rotate(45deg)' }} />GOLD SEAL III
            </span>
          </div>
        </div>
        <div style={{
          flex: 'none', boxSizing: 'border-box', padding: `${h * 0.022}px ${h * 0.03}px`,
          background: 'rgba(0,0,0,.5)', border: `2px solid ${hexA(edge, 0.85)}`,
          display: 'flex', flexDirection: 'column', gap: h * 0.018,
          boxShadow: critical > 0.2 ? `inset 0 0 ${18 * critical}px ${hexA(P.ember, 0.4 * critical)}` : 'none',
        }}>
          <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between' }}>
            <span style={{ font: `500 ${h * 0.028}px/1 'Oswald',sans-serif`, letterSpacing: '.2em', color: '#C08D74' }}>LIFE</span>
            <span style={{
              font: `700 ${h * 0.105}px/1.2 'Cinzel',serif`, color: lpCol,
              textShadow: critical > 0.3 ? `0 0 ${16 * critical}px ${hexA(P.ember, 0.7)}` : 'none',
            }}>{num(lp)}</span>
          </div>
          <div style={{ height: 6, background: 'rgba(0,0,0,.6)', border: `1px solid ${hexA(edge, 0.4)}`, overflow: 'hidden' }}>
            <div style={{
              width: `${frac * 100}%`, height: '100%',
              background: `linear-gradient(90deg,${hexA(P.emberDark, 0.9)},${lpCol})`,
            }} />
          </div>
        </div>
      </div>
      {hurt > 0.01 && <div style={{ position: 'absolute', inset: 0, background: hexA(P.ember, hurt * 0.55), mixBlendMode: 'screen' }} />}
    </div>
  );
}

/* ---------------- cracks and wedges ---------------- */
const WEDGE = [
  { clip: 'polygon(0 0,52% 0,44% 46%,0 38%)', dx: -0.95, dy: -1.15, spin: -20 },
  { clip: 'polygon(52% 0,100% 0,100% 30%,44% 46%)', dx: 1.0, dy: -1.1, spin: 24 },
  { clip: 'polygon(0 38%,44% 46%,30% 100%,0 100%)', dx: -1.2, dy: 0.5, spin: -14 },
  { clip: 'polygon(44% 46%,100% 30%,100% 64%,62% 100%)', dx: 1.2, dy: 0.42, spin: 18 },
  { clip: 'polygon(30% 100%,44% 46%,62% 100%)', dx: -0.1, dy: 1.3, spin: 7 },
  { clip: 'polygon(100% 64%,100% 100%,62% 100%)', dx: 1.05, dy: 1.15, spin: 29 },
];

function Cracks({ o }) {
  if (o <= 0.001) return null;
  const line = (x1, y1, x2, y2, i) => {
    const dx = x2 - x1, dy = y2 - y1;
    const len = Math.sqrt(dx * dx + dy * dy);
    return (
      <div key={i} style={{
        position: 'absolute', left: `${x1}%`, top: `${y1}%`, width: `${len}%`, height: 2.5,
        transformOrigin: '0 50%',
        transform: `rotate(${Math.atan2(dy, dx) * 180 / Math.PI}deg) scaleX(${o})`,
        background: `linear-gradient(90deg,${hexA(P.pale, 0.95)},${hexA(P.emberLit, 0.5)})`,
        boxShadow: `0 0 10px ${hexA(P.emberLit, 0.85)}`,
      }} />
    );
  };
  return (
    <div style={{ position: 'absolute', inset: 0, pointerEvents: 'none', overflow: 'hidden' }}>
      {line(44, 46, 52, 0, 0)}{line(44, 46, 100, 30, 1)}{line(44, 46, 0, 38, 2)}
      {line(44, 46, 30, 100, 3)}{line(44, 46, 62, 100, 4)}{line(62, 100, 100, 64, 5)}
    </div>
  );
}

function Wedges({ fly, spin, fade, drain, fall = 0 }) {
  if (fade <= 0.001) return null;
  return (
    <div style={{ position: 'absolute', left: PC.x, top: PC.y, width: PC.w, height: PC.h, transform: 'translate(-50%,-50%)' }}>
      {WEDGE.map((w, i) => (
        <div key={i} style={{
          position: 'absolute', inset: 0, clipPath: w.clip,
          transform: `translate(${w.dx * fly}px,${w.dy * fly + fall * (60 + i * 26)}px) rotate(${w.spin * spin}deg)`,
          opacity: fade, filter: 'drop-shadow(0 8px 18px rgba(0,0,0,.75))',
        }}>
          <PlayerCard lp={0} drain={drain} />
        </div>
      ))}
    </div>
  );
}

/* ---------------- furniture ---------------- */
function Label({ text, sub, o, rise, tone }) {
  if (o <= 0.001) return null;
  return (
    <div style={{
      position: 'absolute', left: 0, right: 0, top: 40,
      display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 12,
      opacity: o, transform: `translateY(${rise}px)`,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
        <div style={{ width: 68, height: 1, background: `linear-gradient(90deg,transparent,${tone})` }} />
        <span style={{ font: `500 13px/1 'Oswald',sans-serif`, letterSpacing: '.4em', color: tone, whiteSpace: 'nowrap' }}>{text}</span>
        <div style={{ width: 68, height: 1, background: `linear-gradient(270deg,transparent,${tone})` }} />
      </div>
      {sub && <span style={{ font: `400 17px/1 'Spectral',serif`, color: P.muted, whiteSpace: 'nowrap' }}>{sub}</span>}
    </div>
  );
}

function Chip({ text, tone }) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 10, padding: '11px 18px',
      background: 'rgba(0,0,0,.55)', border: `1px solid ${hexA(tone, 0.5)}`, whiteSpace: 'nowrap',
    }}>
      <i style={{ width: 8, height: 8, display: 'block', background: tone, transform: 'rotate(45deg)' }} />
      <span style={{ font: `400 15px/1 'Spectral',serif`, color: '#C8B189' }}>{text}</span>
    </div>
  );
}

/* falling ash — fixed offsets so the frame is reproducible on export */
function Ash({ amount, t }) {
  if (amount <= 0.01) return null;
  return Array.from({ length: 22 }, (_, i) => {
    const ph = (t * 0.5 + i * 0.0417) % 1;
    const x = ((i * 137) % 100) / 100 * W;
    const s = 3 + (i % 4);
    return (
      <div key={i} style={{
        position: 'absolute', left: x + Math.sin(Math.PI * 2 * (ph + i * 0.3)) * 22,
        top: -20 + ph * (H + 60), width: s, height: s,
        transform: 'rotate(45deg)', background: i % 3 ? P.ash : P.emberLit,
        opacity: Math.sin(Math.PI * ph) * 0.55 * amount,
      }} />
    );
  });
}

/* ================= SCENES =================
   ┌───────────┬──────────┬──────────┬──────────────┬──────┐
   │ scene     │ LP       │ attacker │ card         │ ash  │
   ├───────────┼──────────┼──────────┼──────────────┼──────┤
   │ Brink     │ 400      │ raised   │ intact       │ 0    │
   │ Strike    │ 400 → 0  │ lunges   │ cracking     │ 0    │
   │ Break     │ 0        │ gone     │ shatters     │ 0→.5 │
   │ Collapse  │ 0        │ gone     │ falls away   │ 1    │
   │ Defeat    │ 0        │ gone     │ gone         │ 1    │
   └───────────┴──────────┴──────────┴──────────────┴──────┘ */

function SceneBrink() {
  const { progress: p } = useScene();
  const inn = MOTION.enter(seg(p, 0.04, 0.32));
  const beat = 0.5 + 0.5 * Math.sin(Math.PI * 2 * seg(p, 0.2, 1) * 2);
  return (
    <>
      <Field />
      <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: 1 - MOTION.enter(seg(p, 0, 0.2)) }} />
      <Attacker y={FOE.y} glow={0.2 + beat * 0.3} />
      <div style={{
        position: 'absolute', left: PC.x, top: PC.y,
        width: PC.w * 1.9, height: PC.h * 1.5, transform: 'translate(-50%,-50%)', borderRadius: '50%',
        background: `radial-gradient(ellipse at 50% 50%, ${hexA(P.ember, (0.12 + beat * 0.16) * inn)}, transparent 66%)`,
      }} />
      <div style={{
        position: 'absolute', left: PC.x, top: PC.y,
        width: PC.w + 44 + beat * 22, height: PC.h + 44 + beat * 22,
        transform: 'translate(-50%,-50%) rotate(45deg)',
        border: `1px solid ${hexA(P.ember, (1 - beat) * 0.5 * inn)}`,
      }} />
      <div style={{
        position: 'absolute', left: PC.x, top: PC.y, transform: 'translate(-50%,-50%)', opacity: inn,
        filter: `drop-shadow(0 18px 40px rgba(0,0,0,.85)) drop-shadow(0 0 ${16 + beat * 26}px ${hexA(P.ember, 0.3 + beat * 0.3)})`,
      }}>
        <PlayerCard lp={LP_START} />
      </div>
      <Label text="DIRECT ATTACK" sub="400 life left · no monsters to block" o={inn} rise={mix(12, 0, inn)} tone={P.ember} />
    </>
  );
}

function SceneStrike() {
  const { progress: p } = useScene();
  const lunge = MOTION.enter(seg(p, 0.04, 0.42));
  const y = mix(FOE.y, LUNGE_Y, lunge);
  const impact = seg(p, 0.38, 1);
  const drainT = MOTION.enter(seg(p, 0.42, 0.82)) * cfg.drain;
  const lp = mix(LP_START, 0, drainT);
  const hurt = Math.sin(Math.PI * seg(p, 0.38, 0.86));
  const shake = Math.sin(Math.PI * seg(p, 0.38, 1)) * 1;
  const flash = Math.sin(Math.PI * seg(p, 0.36, 0.7));
  const cracks = MOTION.enter(seg(p, 0.52, 1));
  return (
    <>
      <Field shake={shake} />
      {impact > 0 && impact < 1 && (
        <div style={{
          position: 'absolute', left: PC.x, top: PC.y - 40,
          width: mix(140, 720, MOTION.enter(impact)), height: mix(140, 720, MOTION.enter(impact)),
          transform: 'translate(-50%,-50%) rotate(45deg)',
          border: `3px solid ${hexA(P.emberLit, (1 - impact) * 0.85)}`,
        }} />
      )}
      <div style={{
        position: 'absolute', left: PC.x, top: PC.y - 40,
        width: 180 + flash * 900, height: 180 + flash * 900,
        transform: 'translate(-50%,-50%)', borderRadius: '50%',
        background: `radial-gradient(circle, ${hexA(P.pale, flash * 0.5)}, ${hexA(P.ember, flash * 0.18)} 36%, transparent 62%)`,
      }} />
      <div style={{
        position: 'absolute', left: PC.x, top: PC.y, transform: 'translate(-50%,-50%)',
        filter: `drop-shadow(0 18px 40px rgba(0,0,0,.85)) drop-shadow(0 0 ${20 + hurt * 40}px ${hexA(P.ember, 0.35 + hurt * 0.4)})`,
      }}>
        <div style={{ position: 'relative', width: PC.w, height: PC.h }}>
          <PlayerCard lp={lp} hurt={hurt} />
          <Cracks o={cracks} />
        </div>
      </div>
      <Attacker y={y} lunge={lunge} glow={0.5 + flash * 0.5} />
      {/* the damage figure rises off the card */}
      <div style={{
        position: 'absolute', left: PC.x + 192, top: PC.y - 20 - MOTION.enter(seg(p, 0.4, 1)) * 70,
        transform: 'translate(-50%,-50%)',
        opacity: MOTION.enter(seg(p, 0.38, 0.5)) * (1 - MOTION.enter(seg(p, 0.82, 1))),
      }}>
        <span style={{
          font: `700 44px/1.2 'Cinzel',serif`, color: P.emberLit, whiteSpace: 'nowrap',
          textShadow: `0 0 26px ${hexA(P.ember, 0.9)}, 0 3px 12px rgba(0,0,0,.9)`,
        }}>−{num(HIT)}</span>
      </div>
      <Label text="DIRECT ATTACK" o={1 - MOTION.enter(seg(p, 0.3, 0.66))} rise={mix(0, -10, MOTION.enter(seg(p, 0.3, 0.66)))} tone={P.ember} />
    </>
  );
}

function SceneBreak() {
  const { progress: p } = useScene();
  const burst = MOTION.enter(seg(p, 0.1, 1));
  const flash = Math.sin(Math.PI * seg(p, 0, 0.4));
  const shake = Math.sin(Math.PI * seg(p, 0, 0.36)) * 0.85;
  const foeOut = 1 - MOTION.enter(seg(p, 0, 0.34));
  return (
    <>
      <Field shake={shake} dim={MOTION.enter(seg(p, 0.3, 1)) * 0.45} ash={MOTION.enter(seg(p, 0.4, 1)) * 0.6} />
      <div style={{ opacity: foeOut }}>
        <Attacker y={LUNGE_Y} lunge={1} glow={0.5} />
      </div>
      <div style={{
        position: 'absolute', left: PC.x, top: PC.y,
        width: 200 + flash * 1000, height: 200 + flash * 1000,
        transform: 'translate(-50%,-50%)', borderRadius: '50%',
        background: `radial-gradient(circle, ${hexA(P.pale, flash * 0.48)}, ${hexA(P.ember, flash * 0.18)} 36%, transparent 62%)`,
      }} />
      <Wedges fly={burst * 150} spin={burst} drain={MOTION.enter(seg(p, 0.06, 0.8))} fade={1 - c01(seg(p, 0.66, 1) * 0.4)} />
      {Array.from({ length: 12 }, (_, i) => {
        const ph = c01(seg(p, 0.08 + i * 0.026, 1));
        const ang = (i / 12) * Math.PI * 2 + 0.4;
        return (
          <div key={i} style={{
            position: 'absolute',
            left: PC.x + Math.cos(ang) * ph * 220, top: PC.y + Math.sin(ang) * ph * 170 - ph * 34,
            width: 5 + (i % 4), height: 5 + (i % 4), transform: 'rotate(45deg)',
            background: i % 2 ? P.emberLit : P.ash, opacity: Math.sin(Math.PI * ph) * 0.85,
          }} />
        );
      })}
      <Ash amount={MOTION.enter(seg(p, 0.5, 1)) * 0.5} t={p} />
      <Label text="LIFE AT ZERO" o={MOTION.enter(seg(p, 0.46, 0.84))} rise={mix(12, 0, MOTION.enter(seg(p, 0.46, 0.84)))} tone={P.ember} />
    </>
  );
}

function SceneCollapse() {
  const { progress: p } = useScene();
  const fall = MOTION.enter(seg(p, 0, 0.92));
  const sink = MOTION.enter(seg(p, 0.1, 1));
  return (
    <>
      <Field dim={mix(0.45, 0.7, sink)} ash={mix(0.6, 1, sink)} />
      <Wedges fly={150} spin={1} drain={1} fade={1 - MOTION.enter(seg(p, 0.24, 0.9))} fall={fall} />
      <Ash amount={mix(0.5, 1, sink)} t={0.5 + p} />
      <div style={{
        position: 'absolute', left: PC.x, top: PC.y,
        width: mix(300, 1100, sink), height: mix(300, 1100, sink),
        transform: 'translate(-50%,-50%) rotate(45deg)',
        border: `1px solid ${hexA(P.ash, (1 - sink) * 0.35)}`,
      }} />
      <Label text="LIFE AT ZERO" o={1 - MOTION.enter(seg(p, 0.1, 0.5))} rise={mix(0, -10, MOTION.enter(seg(p, 0.1, 0.5)))} tone={P.ember} />
      <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: MOTION.enter(seg(p, 0.8, 1)) * 0.5 }} />
    </>
  );
}

function SceneDefeat() {
  const { progress: p } = useScene();
  const inn = MOTION.enter(seg(p, 0.01, 0.28));
  const chips = MOTION.enter(seg(p, 0.3, 0.58));
  const out = 1 - MOTION.enter(seg(p, 0.94, 1));
  const ring = seg(p, 0.04, 0.44);
  return (
    <>
      <Field dim={0.86} ash={1} />
      <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: 0.66 }} />
      <Ash amount={0.8 * out} t={1 + p} />
      <div style={{
        position: 'absolute', left: PC.x, top: 330,
        width: mix(240, 900, MOTION.enter(ring)), height: mix(240, 900, MOTION.enter(ring)),
        transform: 'translate(-50%,-50%) rotate(45deg)',
        border: `1px solid ${hexA(P.ember, (1 - ring) * 0.4 * out)}`,
      }} />
      <div style={{
        position: 'absolute', left: 0, right: 0, top: 214,
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 26,
        opacity: inn * out, transform: `translateY(${mix(24, 0, inn)}px)`,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 20 }}>
          <div style={{ width: 110, height: 1, background: `linear-gradient(90deg,transparent,${hexA(P.ember, 0.8)})` }} />
          <span style={{ font: `500 14px/1 'Oswald',sans-serif`, letterSpacing: '.42em', color: P.ember, whiteSpace: 'nowrap' }}>THE SEAL HOLDS</span>
          <div style={{ width: 110, height: 1, background: `linear-gradient(270deg,transparent,${hexA(P.ember, 0.8)})` }} />
        </div>
        <span style={{
          font: `700 104px/1.2 'Cinzel',serif`, letterSpacing: '.1em', color: '#E8DCCB',
          whiteSpace: 'nowrap', textShadow: `0 0 70px ${hexA(P.ember, 0.4)}, 0 6px 24px rgba(0,0,0,.9)`,
        }}>DEFEAT</span>
        <span style={{ font: `400 20px/1 'Spectral',serif`, color: P.muted, whiteSpace: 'nowrap' }}>Kestrel_09 wins on turn 11</span>
      </div>
      {cfg.aftermath && (
        <div style={{
          position: 'absolute', left: PC.x, top: 512, transform: 'translateX(-50%)',
          display: 'flex', gap: 14, opacity: chips * out,
        }}>
          <Chip text="−25 RP · Gold Seal III" tone={P.ember} />
          <Chip text="Gold Seal I is your floor" tone={P.light} />
          <Chip text="Daily Seal 4 of 7 kept" tone={P.good} />
        </div>
      )}
      <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: MOTION.enter(seg(p, 0.94, 1)) }} />
    </>
  );
}

/* ================= root ================= */
function PlayerDefeat() {
  const { useTweaks, TweaksPanel, TweakSection, TweakSlider, TweakToggle } = window;
  const [t, setTweak] = useTweaks(window.TWEAK_DEFAULTS);
  cfg.drain = t.drain;
  cfg.aftermath = t.aftermath;
  return (
    <>
      <SceneStage width={W} height={H} scenes={window.OM_SCENES} playback={window.OM_PLAYBACK} bg="#0A0705">
        {{ Brink: SceneBrink, Strike: SceneStrike, Break: SceneBreak, Collapse: SceneCollapse, Defeat: SceneDefeat }}
      </SceneStage>
      <TweaksPanel>
        <TweakSection label="Damage" />
        <TweakSlider label="Life drained" value={t.drain} min={0} max={1} step={0.05}
                     onChange={(v) => setTweak('drain', v)} />
        <TweakSection label="Defeat screen" />
        <TweakToggle label="Aftermath chips" value={t.aftermath} onChange={(v) => setTweak('aftermath', v)} />
        <TweakSection label="Editing" />
        <TweakToggle label="Motion editor" value={t.motionEditor} onChange={(v) => setTweak('motionEditor', v)} />
      </TweaksPanel>
    </>
  );
}

window.PlayerDefeat = PlayerDefeat;
