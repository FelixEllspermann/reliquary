// card-destroy.jsx — RELIQUARY destruction and the trip to the graveyard.
// The card breaks into six wedges, the wedges pull back into a face-down card,
// and that card arcs onto the graveyard stack. Shown for the player's own
// monster; the opponent's side is the same motion mirrored to the top row.

const { SceneStage, useScene } = window;

const P = {
  gold: '#C8A45C', light: '#EBCE8A', pale: '#F8EED6', dark: '#7A5A1E',
  parch1: '#EBE1C7', parch2: '#D9CCAB',
  ember: '#E0603A', emberLit: '#F3C3A6', teal: '#8FC6D2', violet: '#B9A3E0',
  ash: '#8A857B', good: '#7ACD96', dim: '#9C8A6A', muted: '#A2917A',
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
const CW = 132, CH = 185;
const ROW_MINE = 452, ROW_FOE = 178;
const ZONE_X = [372, 470, 568, 666, 764];
const DOOMED = { x: 568, y: ROW_MINE };
const GRAVE = { x: 1128, y: 556 };
const cfg = { arc: 1, callout: true };

/* ---------------- field ---------------- */
function Field({ dim = 0, shake = 0 }) {
  const sx = Math.sin(shake * Math.PI * 12) * shake * 7;
  const sy = Math.cos(shake * Math.PI * 9) * shake * 5;
  return (
    <div style={{ position: 'absolute', inset: 0, overflow: 'hidden', transform: `translate(${sx}px,${sy}px)` }}>
      <div style={{ position: 'absolute', inset: -40, background: 'radial-gradient(ellipse 980px 620px at 50% 48%, #2A1C12, #0A0705 78%)' }} />
      <div style={{
        position: 'absolute', inset: 0,
        background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.04) 0 1px,transparent 1px 26px),repeating-linear-gradient(-45deg,rgba(200,164,92,.04) 0 1px,transparent 1px 26px)',
      }} />
      <div style={{ position: 'absolute', left: 0, right: 0, top: 316, height: 1, background: hexA(P.gold, 0.22) }} />
      <div style={{
        position: 'absolute', left: W / 2, top: 316, width: 22, height: 22,
        transform: 'translate(-50%,-50%) rotate(45deg)', background: '#0A0705', border: `1px solid ${hexA(P.gold, 0.4)}`,
      }} />
      {[ROW_FOE, ROW_MINE].map((y) => ZONE_X.map((x, i) => (
        <div key={y + '-' + i} style={{
          position: 'absolute', left: x, top: y, width: CW + 8, height: CH + 8,
          transform: 'translate(-50%,-50%)', boxSizing: 'border-box', border: `1px solid ${hexA(P.gold, 0.13)}`,
        }} />
      )))}
      <div style={{ position: 'absolute', inset: 0, boxShadow: 'inset 0 0 190px rgba(0,0,0,.9)' }} />
      {dim > 0 && <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: dim * 0.5 }} />}
    </div>
  );
}

/* ---------------- card faces ---------------- */
function Back({ w = CW, h = CH }) {
  return (
    <div style={{
      position: 'relative', width: w, height: h, borderRadius: 5, overflow: 'hidden',
      background: 'radial-gradient(ellipse at 50% 50%, #4E2A18, #1C0E08 78%)', border: `2px solid ${P.gold}`,
    }}>
      <div style={{
        position: 'absolute', inset: 0,
        background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.13) 0 1px,transparent 1px 10px),repeating-linear-gradient(-45deg,rgba(200,164,92,.13) 0 1px,transparent 1px 10px)',
      }} />
      <div style={{ position: 'absolute', inset: 4, border: `1px solid ${hexA(P.gold, 0.5)}`, borderRadius: 3 }} />
      <div style={{ position: 'absolute', left: '50%', top: '50%', width: w * 0.47, height: w * 0.47, transform: 'translate(-50%,-50%) rotate(45deg)', border: `2px solid ${hexA(P.gold, 0.6)}` }} />
      <div style={{
        position: 'absolute', left: '50%', top: '50%', width: w * 0.23, height: w * 0.23,
        transform: 'translate(-50%,-50%) rotate(45deg)',
        background: `linear-gradient(135deg,${hexA(P.gold, 0.35)},${hexA(P.gold, 0.05)})`, border: `1px solid ${hexA(P.gold, 0.7)}`,
      }} />
      <div style={{ position: 'absolute', left: '50%', top: '50%', width: w * 0.1, height: w * 0.1, transform: 'translate(-50%,-50%) rotate(45deg)', background: `linear-gradient(135deg,${P.light},${P.dark})` }} />
    </div>
  );
}

function Front({ d, w = CW, h = CH, hurt = 0, drain = 0 }) {
  const pad = w * 0.104, inner = w - pad * 2, art = inner * 0.92;
  return (
    <div style={{
      position: 'relative', width: w, height: h, borderRadius: 5, overflow: 'hidden',
      boxSizing: 'border-box', padding: `${h * 0.03}px ${pad}px`,
      background: 'linear-gradient(165deg,#332315,#150d07 55%,#251809)',
      border: `2px solid ${mixHex(d.edge, P.ash, drain)}`,
      filter: drain > 0.01 ? `saturate(${1 - drain * 0.85}) brightness(${1 - drain * 0.3})` : 'none',
    }}>
      <div style={{ position: 'absolute', inset: 3, border: `1px solid ${hexA(d.edge, 0.4)}`, borderRadius: 3 }} />
      <div style={{ position: 'relative', width: inner, display: 'flex', flexDirection: 'column', gap: h * 0.014 }}>
        <div style={{ height: h * 0.082, display: 'flex', alignItems: 'center', gap: 3 }}>
          <div style={{
            flex: 1, height: '100%', display: 'flex', alignItems: 'center', padding: '0 6px', boxSizing: 'border-box',
            background: 'linear-gradient(180deg,#42301C,#22150A)',
            borderTop: `1px solid ${P.gold}`, borderBottom: `1px solid ${P.gold}`,
            clipPath: 'polygon(0 0,100% 0,calc(100% - 6px) 100%,6px 100%)', overflow: 'hidden',
          }}>
            <div style={{ width: '72%', height: 3, background: `linear-gradient(90deg,${hexA(d.edge, 0.85)},${hexA(d.edge, 0.25)})` }} />
          </div>
          <div style={{
            position: 'relative', width: h * 0.075, height: h * 0.082, flex: 'none',
            background: `linear-gradient(160deg,${P.light},#8E6A22)`,
            clipPath: 'polygon(50% 0,100% 20%,100% 66%,50% 100%,0 66%,0 20%)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
          }}>
            <div style={{ position: 'absolute', inset: 1.5, background: 'linear-gradient(160deg,#3B2A10,#180F04)', clipPath: 'polygon(50% 0,100% 20%,100% 66%,50% 100%,0 66%,0 20%)' }} />
            <span style={{ position: 'relative', font: `700 ${h * 0.05}px/1.2 'Cinzel',serif`, color: '#F3DDA4' }}>{d.lv}</span>
          </div>
        </div>
        <div style={{
          width: art, height: art, alignSelf: 'center', boxSizing: 'border-box', padding: 3,
          background: 'linear-gradient(160deg,#3E2C16,#1A1108)', border: `2px solid ${P.gold}`,
        }}>
          <div style={{ width: '100%', height: '100%', boxSizing: 'border-box', overflow: 'hidden', border: `1px solid ${hexA(P.gold, 0.65)}` }}>
            <img src="uploads/artwork-1785612438938.png" alt="" style={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }} />
          </div>
        </div>
        <div style={{ height: h * 0.052, display: 'flex', gap: 2 }}>
          <div style={{
            flex: 'none', padding: '0 4px', display: 'flex', alignItems: 'center',
            background: 'linear-gradient(180deg,#E2C685,#9C7526)', color: '#1E1405',
            font: `600 ${h * 0.028}px/1 'Oswald',sans-serif`, letterSpacing: '.08em',
          }}>{d.kind}</div>
          <div style={{
            flex: 1, display: 'flex', alignItems: 'center', padding: '0 4px',
            background: 'rgba(0,0,0,.35)', border: `1px solid ${hexA(P.gold, 0.45)}`,
          }}>
            <span style={{ display: 'flex', alignItems: 'center', gap: 3, font: `500 ${h * 0.028}px/1 'Oswald',sans-serif`, letterSpacing: '.08em', color: '#E4D3AE' }}>
              <i style={{ width: 4, height: 4, display: 'block', background: d.attrC, transform: 'rotate(45deg)' }} />{d.attr}
            </span>
          </div>
        </div>
        <div style={{
          height: h * 0.16, boxSizing: 'border-box', padding: '4px 5px',
          background: `linear-gradient(180deg,${P.parch1},${P.parch2})`, border: '1px solid #8C7440',
          display: 'flex', flexDirection: 'column', gap: 2.5,
        }}>
          {['94%', '86%', '91%', '56%'].map((w2, i) => (
            <div key={i} style={{ width: w2, height: 2.5, background: 'rgba(46,36,23,.32)' }} />
          ))}
        </div>
        <div style={{ height: h * 0.07, display: 'flex', gap: 2 }}>
          <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 3, background: 'rgba(0,0,0,.4)', border: `1px solid ${hexA(P.ember, 0.5)}` }}>
            <span style={{ font: `500 ${h * 0.026}px/1 'Oswald',sans-serif`, letterSpacing: '.1em', color: '#C08D74' }}>DMG</span>
            <span style={{ font: `700 ${h * 0.04}px/1.2 'Cinzel',serif`, color: P.emberLit }}>{d.dmg}</span>
          </div>
          <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 3, background: 'rgba(0,0,0,.4)', border: `1px solid ${hexA(P.teal, 0.5)}` }}>
            <span style={{ font: `500 ${h * 0.026}px/1 'Oswald',sans-serif`, letterSpacing: '.1em', color: '#7FAAB4' }}>DEF</span>
            <span style={{ font: `700 ${h * 0.04}px/1.2 'Cinzel',serif`, color: '#B9E6F0' }}>{d.def}</span>
          </div>
        </div>
      </div>
      {hurt > 0.01 && <div style={{ position: 'absolute', inset: 0, background: hexA(P.ember, hurt * 0.5), mixBlendMode: 'screen' }} />}
    </div>
  );
}

function mixHex(a, b, t) {
  const pa = a.replace('#', ''), pb = b.replace('#', '');
  const ch = (s, i) => parseInt(s.slice(i, i + 2), 16);
  const r = Math.round(mix(ch(pa, 0), ch(pb, 0), t));
  const g = Math.round(mix(ch(pa, 2), ch(pb, 2), t));
  const bl = Math.round(mix(ch(pa, 4), ch(pb, 4), t));
  return `rgb(${r},${g},${bl})`;
}

const MINE = { edge: P.gold, kind: 'MONSTER', attr: 'FIRE', attrC: P.ember, lv: 2, dmg: '1800', def: '1500' };
const NEIGHBOUR = { edge: P.teal, kind: 'MONSTER', attr: 'WATER', attrC: P.teal, lv: 1, dmg: '1200', def: '900' };
const FOE_CARD = { edge: P.violet, kind: 'MONSTER', attr: 'DARK', attrC: P.violet, lv: 3, dmg: '2600', def: '2100' };

/* ---------------- fracture lines, then the wedges ----------------
   Six wedges cut from one card, each carrying its own clipped copy so the
   artwork breaks with the frame. `fly` pushes each along its centroid. */
const WEDGE = [
  { clip: 'polygon(0 0,52% 0,44% 46%,0 38%)', dx: -0.95, dy: -1.15, spin: -22 },
  { clip: 'polygon(52% 0,100% 0,100% 30%,44% 46%)', dx: 1.0, dy: -1.1, spin: 26 },
  { clip: 'polygon(0 38%,44% 46%,30% 100%,0 100%)', dx: -1.2, dy: 0.5, spin: -15 },
  { clip: 'polygon(44% 46%,100% 30%,100% 64%,62% 100%)', dx: 1.2, dy: 0.42, spin: 19 },
  { clip: 'polygon(30% 100%,44% 46%,62% 100%)', dx: -0.1, dy: 1.3, spin: 8 },
  { clip: 'polygon(100% 64%,100% 100%,62% 100%)', dx: 1.05, dy: 1.15, spin: 31 },
];

function Cracks({ o }) {
  if (o <= 0.001) return null;
  const line = (x1, y1, x2, y2, i) => {
    const dx = x2 - x1, dy = y2 - y1;
    const len = Math.sqrt(dx * dx + dy * dy);
    return (
      <div key={i} style={{
        position: 'absolute', left: `${x1}%`, top: `${y1}%`,
        width: `${len}%`, height: 2, transformOrigin: '0 50%',
        transform: `rotate(${Math.atan2(dy, dx) * 180 / Math.PI}deg) scaleX(${o})`,
        background: `linear-gradient(90deg,${hexA(P.pale, 0.9)},${hexA(P.emberLit, 0.5)})`,
        boxShadow: `0 0 8px ${hexA(P.emberLit, 0.8)}`,
      }} />
    );
  };
  return (
    <div style={{ position: 'absolute', inset: 0, pointerEvents: 'none', overflow: 'hidden' }}>
      {line(44, 46, 52, 0, 0)}
      {line(44, 46, 100, 30, 1)}
      {line(44, 46, 0, 38, 2)}
      {line(44, 46, 30, 100, 3)}
      {line(44, 46, 62, 100, 4)}
      {line(62, 100, 100, 64, 5)}
    </div>
  );
}

function Wedges({ x, y, fly, spin, fade, drain, scale = 1 }) {
  if (fade <= 0.001) return null;
  return (
    <div style={{ position: 'absolute', left: x, top: y, width: CW, height: CH, transform: `translate(-50%,-50%) scale(${scale})` }}>
      {WEDGE.map((w, i) => (
        <div key={i} style={{
          position: 'absolute', inset: 0, clipPath: w.clip,
          transform: `translate(${w.dx * fly}px,${w.dy * fly}px) rotate(${w.spin * spin}deg)`,
          opacity: fade,
          filter: `drop-shadow(0 6px 14px rgba(0,0,0,.7))`,
        }}>
          <Front d={MINE} drain={drain} />
        </div>
      ))}
    </div>
  );
}

/* ---------------- the graveyard ---------------- */
function Graveyard({ count, o = 1, glow = 0, pop = 0 }) {
  const stack = Math.min(6, count);
  return (
    <div style={{ position: 'absolute', left: GRAVE.x, top: GRAVE.y, opacity: o }}>
      <div style={{
        position: 'absolute', left: 0, top: 0, width: CW * 1.9, height: CH * 1.1,
        transform: 'translate(-50%,-50%)', borderRadius: '50%',
        background: `radial-gradient(ellipse at 50% 50%, ${hexA(P.violet, 0.1 + glow * 0.34)}, transparent 68%)`,
      }} />
      <div style={{
        position: 'absolute', left: 0, top: 0, width: CW * 0.72, height: CH * 0.72,
        transform: `translate(-50%,-50%) rotate(45deg) scale(${1 + pop * 0.35})`,
        border: `1px solid ${hexA(P.violet, 0.3 + glow * 0.4)}`,
      }} />
      {Array.from({ length: stack }, (_, i) => (
        <div key={i} style={{
          position: 'absolute', left: 0, top: -i * 3,
          transform: `translate(-50%,-50%) rotate(${(i % 2 ? 1 : -1) * (1.2 + i * 0.5)}deg) scale(.62)`,
          filter: 'drop-shadow(0 6px 14px rgba(0,0,0,.7))',
        }}>
          <Back />
        </div>
      ))}
      <div style={{
        position: 'absolute', left: 0, top: CH * 0.38, transform: 'translateX(-50%)',
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 7, whiteSpace: 'nowrap',
      }}>
        <span style={{ font: `500 10px/1 'Oswald',sans-serif`, letterSpacing: '.26em', color: P.dim }}>GRAVEYARD</span>
        <span style={{
          font: `700 ${mix(22, 27, pop)}px/1.2 'Cinzel',serif`,
          color: pop > 0.05 ? '#EFE7FA' : '#D6D0EA',
          textShadow: pop > 0.05 ? `0 0 ${18 * pop}px ${hexA(P.violet, 0.8)}` : 'none',
        }}>{count}</span>
      </div>
    </div>
  );
}

/* ---------------- furniture ---------------- */
function Label({ text, sub, o, rise, tone }) {
  if (o <= 0.001) return null;
  return (
    <div style={{
      position: 'absolute', left: 0, right: 0, top: 46,
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

/* the rest of the board, so the destroyed card has somewhere to be destroyed */
function Board({ dim = 0 }) {
  return (
    <>
      <div style={{ position: 'absolute', left: ZONE_X[2], top: ROW_FOE, transform: 'translate(-50%,-50%)', opacity: 1 - dim * 0.45 }}>
        <Front d={FOE_CARD} />
      </div>
      <div style={{ position: 'absolute', left: ZONE_X[1], top: ROW_MINE, transform: 'translate(-50%,-50%)', opacity: 1 - dim * 0.45 }}>
        <Front d={NEIGHBOUR} />
      </div>
    </>
  );
}

/* ================= SCENES =================
   ┌──────────┬──────────┬────────┬──────────────┬───────┐
   │ scene    │ wedges   │ drain  │ courier card │ grave │
   ├──────────┼──────────┼────────┼──────────────┼───────┤
   │ Struck   │ intact   │ 0      │ —            │ 7     │
   │ Shatter  │ 0 → out  │ 0 → 1  │ —            │ 7     │
   │ Gather   │ out → 0  │ 1      │ appears      │ 7     │
   │ Flight   │ gone     │ 1      │ arcs across  │ 7     │
   │ Land     │ gone     │ 1      │ on the pile  │ 7 → 8 │
   └──────────┴──────────┴────────┴──────────────┴───────┘ */

function SceneStruck() {
  const { progress: p } = useScene();
  const inn = MOTION.enter(seg(p, 0.04, 0.3));
  const hurt = Math.sin(Math.PI * seg(p, 0.24, 0.72));
  const shake = Math.sin(Math.PI * seg(p, 0.26, 0.9)) * 0.8;
  const cracks = MOTION.enter(seg(p, 0.44, 1));
  const hit = seg(p, 0.24, 0.78);
  return (
    <>
      <Field shake={shake} />
      <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: 1 - MOTION.enter(seg(p, 0, 0.2)) }} />
      <Board />
      <Graveyard count={7} />
      {hit > 0 && hit < 1 && (
        <div style={{
          position: 'absolute', left: DOOMED.x, top: DOOMED.y,
          width: mix(110, 470, MOTION.enter(hit)), height: mix(110, 470, MOTION.enter(hit)),
          transform: 'translate(-50%,-50%) rotate(45deg)',
          border: `2px solid ${hexA(P.emberLit, (1 - hit) * 0.85)}`,
        }} />
      )}
      <div style={{
        position: 'absolute', left: DOOMED.x, top: DOOMED.y, transform: 'translate(-50%,-50%)',
        filter: `drop-shadow(0 0 ${14 + hurt * 30}px ${hexA(P.ember, 0.25 + hurt * 0.5)})`,
      }}>
        <div style={{ position: 'relative', width: CW, height: CH }}>
          <Front d={MINE} hurt={hurt} />
          <Cracks o={cracks} />
        </div>
      </div>
      <Label text="DESTROYED" sub="Sentinel loses the clash by 800" o={inn} rise={mix(12, 0, inn)} tone={P.ember} />
    </>
  );
}

function SceneShatter() {
  const { progress: p } = useScene();
  const burst = MOTION.enter(seg(p, 0.08, 1));
  const flash = Math.sin(Math.PI * seg(p, 0.02, 0.44));
  const shake = Math.sin(Math.PI * seg(p, 0, 0.4)) * 0.7;
  return (
    <>
      <Field dim={MOTION.enter(seg(p, 0.3, 1)) * 0.5} shake={shake} />
      <Board dim={MOTION.enter(seg(p, 0.3, 1))} />
      <Graveyard count={7} glow={MOTION.enter(seg(p, 0.5, 1)) * 0.3} />
      <div style={{
        position: 'absolute', left: DOOMED.x, top: DOOMED.y,
        width: 140 + flash * 560, height: 140 + flash * 560,
        transform: 'translate(-50%,-50%)', borderRadius: '50%',
        background: `radial-gradient(circle, ${hexA(P.pale, flash * 0.42)}, ${hexA(P.ember, flash * 0.16)} 38%, transparent 64%)`,
      }} />
      <Wedges x={DOOMED.x} y={DOOMED.y} fly={burst * 118} spin={burst} drain={MOTION.enter(seg(p, 0.1, 0.86))} fade={1 - c01(seg(p, 0.72, 1) * 0.35)} />
      {/* ash flecks — fixed offsets, so the frame is reproducible on export */}
      {Array.from({ length: 9 }, (_, i) => {
        const ph = c01(seg(p, 0.1 + i * 0.03, 1));
        const ang = (i / 9) * Math.PI * 2 + 0.6;
        return (
          <div key={i} style={{
            position: 'absolute',
            left: DOOMED.x + Math.cos(ang) * ph * 170,
            top: DOOMED.y + Math.sin(ang) * ph * 130 - ph * 30,
            width: 5 + (i % 3), height: 5 + (i % 3), transform: 'rotate(45deg)',
            background: i % 2 ? P.emberLit : P.ash, opacity: Math.sin(Math.PI * ph) * 0.8,
          }} />
        );
      })}
      <Label text="DESTROYED" o={1 - MOTION.enter(seg(p, 0.2, 0.6))} rise={mix(0, -10, MOTION.enter(seg(p, 0.2, 0.6)))} tone={P.ember} />
    </>
  );
}

function SceneGather() {
  const { progress: p } = useScene();
  const pull = MOTION.enter(seg(p, 0.06, 0.68));
  const born = MOTION.pop(seg(p, 0.52, 0.92));
  const swirl = seg(p, 0, 0.8);
  return (
    <>
      <Field dim={0.5} />
      <Board dim={1} />
      <Graveyard count={7} glow={0.3} />
      <Wedges x={DOOMED.x} y={DOOMED.y}
        fly={mix(118, 0, pull)} spin={1 - pull}
        drain={1} fade={1 - MOTION.enter(seg(p, 0.44, 0.8))}
        scale={mix(1, 0.72, pull)} />
      {swirl > 0 && swirl < 1 && (
        <div style={{
          position: 'absolute', left: DOOMED.x, top: DOOMED.y,
          width: mix(420, 90, MOTION.enter(swirl)), height: mix(420, 90, MOTION.enter(swirl)),
          transform: `translate(-50%,-50%) rotate(${45 + swirl * 120}deg)`,
          border: `1px solid ${hexA(P.violet, (1 - Math.abs(swirl - 0.5) * 2) * 0.6)}`,
        }} />
      )}
      {born > 0.001 && (
        <div style={{
          position: 'absolute', left: DOOMED.x, top: DOOMED.y,
          transform: `translate(-50%,-50%) rotate(${mix(-26, 0, c01(born))}deg) scale(${0.72 * born})`,
          filter: `drop-shadow(0 10px 24px rgba(0,0,0,.8)) drop-shadow(0 0 20px ${hexA(P.violet, 0.4)})`,
        }}>
          <Back />
        </div>
      )}
      <Label text="TO THE GRAVEYARD" o={MOTION.enter(seg(p, 0.5, 0.9))} rise={mix(12, 0, MOTION.enter(seg(p, 0.5, 0.9)))} tone={P.violet} />
    </>
  );
}

function SceneFlight() {
  const { progress: p } = useScene();
  // a single eased arc: the card rises out of the field, then drops onto the pile
  const t = MOTION.drift(p);
  const x = mix(DOOMED.x, GRAVE.x, t);
  const y = mix(DOOMED.y, GRAVE.y, t) - Math.sin(Math.PI * t) * 168 * cfg.arc;
  const scale = mix(0.72, 0.62, t) * (1 + Math.sin(Math.PI * t) * 0.16);
  return (
    <>
      <Field dim={mix(0.5, 0.16, t)} />
      <Board dim={1 - t * 0.8} />
      <Graveyard count={7} glow={mix(0.3, 0.62, t)} />
      {/* the trail: fixed samples behind the card along the same arc */}
      {[0.1, 0.19, 0.29, 0.4].map((lag, i) => {
        const tt = c01(t - lag);
        if (tt <= 0.001) return null;
        const e = MOTION.drift(tt / Math.max(0.001, 1 - lag)) * (1 - lag) + 0;
        const lx = mix(DOOMED.x, GRAVE.x, tt);
        const ly = mix(DOOMED.y, GRAVE.y, tt) - Math.sin(Math.PI * tt) * 168 * cfg.arc;
        return (
          <div key={i} style={{
            position: 'absolute', left: lx, top: ly, width: 16 - i * 3, height: 16 - i * 3,
            transform: 'translate(-50%,-50%) rotate(45deg)',
            background: hexA(P.violet, (0.5 - i * 0.1) * (1 - c01(seg(p, 0.86, 1)))),
          }} />
        );
      })}
      <div style={{
        position: 'absolute', left: x, top: y,
        transform: `translate(-50%,-50%) rotate(${mix(0, 382, MOTION.enter(p))}deg) scale(${scale})`,
        filter: `drop-shadow(0 ${mix(10, 22, Math.sin(Math.PI * t))}px 26px rgba(0,0,0,.8)) drop-shadow(0 0 ${16 + t * 18}px ${hexA(P.violet, 0.35)})`,
      }}>
        <Back />
      </div>
      <Label text="TO THE GRAVEYARD" o={1 - MOTION.enter(seg(p, 0.6, 1))} rise={mix(0, -10, MOTION.enter(seg(p, 0.6, 1)))} tone={P.violet} />
    </>
  );
}

function SceneLand() {
  const { progress: p } = useScene();
  const settle = MOTION.enter(seg(p, 0, 0.26));
  const pop = Math.sin(Math.PI * seg(p, 0.12, 0.56));
  const ring = seg(p, 0.1, 0.66);
  const chip = MOTION.enter(seg(p, 0.3, 0.6));
  const out = 1 - MOTION.enter(seg(p, 0.9, 1));
  return (
    <>
      <Field dim={mix(0.16, 0, settle)} />
      <Board dim={0.2 * (1 - settle)} />
      <Graveyard count={p > 0.12 ? 8 : 7} glow={0.62 * out * (0.6 + pop * 0.4)} pop={pop} />
      {ring > 0 && ring < 1 && (
        <div style={{
          position: 'absolute', left: GRAVE.x, top: GRAVE.y,
          width: mix(90, 340, MOTION.enter(ring)), height: mix(90, 340, MOTION.enter(ring)),
          transform: 'translate(-50%,-50%) rotate(45deg)',
          border: `2px solid ${hexA(P.violet, (1 - ring) * 0.7)}`,
        }} />
      )}
      {/* the arriving card drops the last few pixels onto the stack */}
      <div style={{
        position: 'absolute', left: GRAVE.x, top: GRAVE.y - mix(26, 0, settle),
        transform: `translate(-50%,-50%) rotate(${mix(22, 1.5, settle)}deg) scale(.62)`,
        opacity: 1 - MOTION.enter(seg(p, 0.16, 0.34)),
        filter: 'drop-shadow(0 10px 22px rgba(0,0,0,.8))',
      }}>
        <Back />
      </div>
      {cfg.callout && (
        <div style={{
          position: 'absolute', left: GRAVE.x, top: GRAVE.y - 168, transform: 'translateX(-50%)',
          opacity: chip * out,
        }}>
          <div style={{
            display: 'flex', alignItems: 'center', gap: 11, padding: '10px 18px',
            background: 'rgba(0,0,0,.55)', border: `1px solid ${hexA(P.violet, 0.6)}`, whiteSpace: 'nowrap',
          }}>
            <i style={{ width: 8, height: 8, display: 'block', background: P.violet, transform: 'rotate(45deg)' }} />
            <span style={{ font: `400 15px/1 'Spectral',serif`, color: '#C8B189' }}>1 card sent · 8 in graveyard</span>
          </div>
        </div>
      )}
      <Label text="RESOLVED" o={MOTION.enter(seg(p, 0.42, 0.7)) * out} rise={mix(12, 0, MOTION.enter(seg(p, 0.42, 0.7)))} tone={P.good} />
      <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: MOTION.enter(seg(p, 0.9, 1)) }} />
    </>
  );
}

/* ================= root ================= */
function CardDestroy() {
  const { useTweaks, TweaksPanel, TweakSection, TweakSlider, TweakToggle } = window;
  const [t, setTweak] = useTweaks(window.TWEAK_DEFAULTS);
  cfg.arc = t.arc;
  cfg.callout = t.callout;
  return (
    <>
      <SceneStage width={W} height={H} scenes={window.OM_SCENES} playback={window.OM_PLAYBACK} bg="#0A0705">
        {{ Struck: SceneStruck, Shatter: SceneShatter, Gather: SceneGather, Flight: SceneFlight, Land: SceneLand }}
      </SceneStage>
      <TweaksPanel>
        <TweakSection label="Flight" />
        <TweakSlider label="Arc height" value={t.arc} min={0} max={1.8} step={0.05}
                     onChange={(v) => setTweak('arc', v)} />
        <TweakSection label="Landing" />
        <TweakToggle label="Counter callout" value={t.callout} onChange={(v) => setTweak('callout', v)} />
        <TweakSection label="Editing" />
        <TweakToggle label="Motion editor" value={t.motionEditor} onChange={(v) => setTweak('motionEditor', v)} />
      </TweaksPanel>
    </>
  );
}

window.CardDestroy = CardDestroy;
