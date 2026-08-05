// vault-enter.jsx — RELIQUARY: login → vault. Scenes for animations-v2.
// One continuous camera push through a seal that unlocks, rotates and opens.
// `depth` (0 → 1) is the through-line: how far the camera has travelled inward.

const { SceneStage, useScene } = window;

const P = {
  gold: '#C8A45C', light: '#EBCE8A', pale: '#F8EED6',
  dark: '#7A5A1E', deep: '#3B2A10',
  parch1: '#EBE1C7', parch2: '#D9CCAB', ink: '#2E2417',
  teal: '#8FC6D2', violet: '#B9A3E0', ember: '#E0603A', good: '#7ACD96',
  muted: '#A2917A', dim: '#9C8A6A',
};

const c01 = (v) => (v < 0 ? 0 : v > 1 ? 1 : v);
const seg = (p, a, b) => c01((p - a) / (b - a));
const mix = (a, b, t) => a + (b - a) * t;

const MOTION = {
  enter: (p) => 1 - Math.pow(1 - c01(p), 3),
  drift: (p) => 0.5 - 0.5 * Math.cos(Math.PI * c01(p)),
  pop: (p) => { const t = c01(p), s = 1.9; return 1 + (s + 1) * Math.pow(t - 1, 3) + s * Math.pow(t - 1, 2); },
};

const W = 1280, H = 720;
const CX = W / 2, CY = H / 2;

/* ---------------- vault backdrop ---------------- */
function Backdrop({ depth, glow = 0 }) {
  const s = 1 + depth * 1.9;
  return (
    <div style={{ position: 'absolute', inset: 0, overflow: 'hidden' }}>
      <div style={{ position: 'absolute', inset: -200, background: `radial-gradient(ellipse 1100px 700px at 50% 48%, #2A1C12, #0A0705 76%)` }} />
      <div style={{
        position: 'absolute', inset: -200,
        transform: `scale(${1 + depth * 0.5})`, transformOrigin: '50% 48%',
        background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.045) 0 1px,transparent 1px 28px),repeating-linear-gradient(-45deg,rgba(200,164,92,.045) 0 1px,transparent 1px 28px)',
      }} />
      {/* concentric ornament rings receding into the vault */}
      {[980, 700, 470, 300].map((base, i) => (
        <div key={i} style={{
          position: 'absolute', left: CX, top: CY,
          width: base * s, height: base * s,
          transform: 'translate(-50%,-50%) rotate(45deg)',
          border: `1px solid rgba(200,164,92,${0.16 - i * 0.025 + glow * 0.14})`,
        }} />
      ))}
      <div style={{
        position: 'absolute', left: CX, top: CY, width: 300 * s, height: 300 * s,
        transform: 'translate(-50%,-50%)',
        background: `radial-gradient(circle at 50% 50%, rgba(235,206,138,${0.05 + glow * 0.3}), transparent 66%)`,
      }} />
      <div style={{ position: 'absolute', inset: -200, boxShadow: 'inset 0 0 240px rgba(0,0,0,.88)' }} />
    </div>
  );
}

/* ---------------- the seal ---------------- */
// r = radius in px, turn = ring rotation in degrees, split = how far the two
// halves have parted (0 = shut, 1 = fully open), lit = emissive amount
function Seal({ r, turn, split, lit, tumblers = 0 }) {
  const gap = split * r * 1.5;
  const half = (side) => (
    <div style={{
      position: 'absolute', left: 0, top: 0, width: r * 2, height: r * 2,
      transform: `translateX(${side * gap}px)`,
      clipPath: side < 0 ? 'inset(0 50% 0 0)' : 'inset(0 0 0 50%)',
    }}>
      <div style={{
        position: 'absolute', inset: 0, borderRadius: '50%',
        background: `radial-gradient(circle at 36% 30%, ${P.pale}, ${P.gold} 46%, ${P.deep} 92%)`,
        boxShadow: `inset 0 0 0 ${r * 0.05}px ${P.light}, inset 0 0 0 ${r * 0.07}px ${P.deep}`,
      }} />
      <div style={{
        position: 'absolute', left: '50%', top: '50%', width: r * 1.1, height: r * 1.1,
        transform: 'translate(-50%,-50%) rotate(45deg)', border: `${r * 0.045}px solid ${P.deep}`,
      }} />
      <div style={{
        position: 'absolute', left: '50%', top: '50%', width: r * 0.6, height: r * 0.6,
        transform: 'translate(-50%,-50%) rotate(45deg)', border: `${r * 0.035}px solid ${P.deep}`,
        background: 'rgba(59,42,16,.2)',
      }} />
    </div>
  );

  return (
    <div style={{
      position: 'absolute', left: CX, top: CY, width: r * 2, height: r * 2,
      transform: 'translate(-50%,-50%)',
      filter: lit > 0 ? `drop-shadow(0 0 ${20 + lit * 60}px rgba(235,206,138,${0.2 + lit * 0.5}))` : 'none',
    }}>
      {/* outer tumbler ring — rotates as the lock turns */}
      <div style={{
        position: 'absolute', inset: -r * 0.18, borderRadius: '50%',
        border: `${r * 0.035}px solid rgba(200,164,92,.55)`,
        transform: `rotate(${turn}deg)`,
      }}>
        {[0, 60, 120, 180, 240, 300].map((a, i) => {
          const on = tumblers > i / 6;
          return (
            <div key={a} style={{
              position: 'absolute', left: '50%', top: '50%',
              width: r * 0.11, height: r * 0.11,
              transform: `translate(-50%,-50%) rotate(${a}deg) translateY(${-r * 1.18}px) rotate(45deg)`,
              background: on ? P.light : 'rgba(200,164,92,.28)',
              boxShadow: on ? `0 0 ${r * 0.14}px rgba(235,206,138,.9)` : 'none',
            }} />
          );
        })}
      </div>
      {half(-1)}
      {half(1)}
      {/* core — the last thing to give way */}
      <div style={{
        position: 'absolute', left: '50%', top: '50%',
        width: r * 0.26 * (1 - split), height: r * 0.26 * (1 - split),
        transform: `translate(-50%,-50%) rotate(${45 + turn}deg)`,
        background: `linear-gradient(135deg,${P.pale},${P.dark})`,
        boxShadow: `0 0 ${r * 0.3}px rgba(235,206,138,${0.5 + lit * 0.5})`,
        opacity: 1 - split,
      }} />
    </div>
  );
}

/* ---------------- copy ---------------- */
function Line({ eyebrow, head, o, rise }) {
  if (o <= 0.001) return null;
  return (
    <div style={{
      position: 'absolute', left: 0, right: 0, top: 96,
      display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 16,
      opacity: o, transform: `translateY(${rise}px)`,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
        <div style={{ width: 70, height: 1, background: `linear-gradient(90deg,transparent,${P.gold})` }} />
        <span style={{ font: `500 13px/1 'Oswald',sans-serif`, letterSpacing: '.38em', color: P.dim, whiteSpace: 'nowrap' }}>{eyebrow}</span>
        <div style={{ width: 70, height: 1, background: `linear-gradient(270deg,transparent,${P.gold})` }} />
      </div>
      <span style={{ font: `700 50px/1.2 'Cinzel',serif`, letterSpacing: '.05em', color: '#F1DFB8', whiteSpace: 'nowrap' }}>{head}</span>
    </div>
  );
}

/* ================= SCENES =================
   depth  0 → .18 → .48 → 1
   split  0 →  0  → .55 → 1
   turn   0 → 300 → 420 → 460                                             */

function SceneApproach() {
  const { progress: p } = useScene();
  const d = MOTION.drift(p) * 0.18;
  const r = mix(150, 172, MOTION.drift(p));
  const settle = MOTION.enter(seg(p, 0, 0.4));
  return (
    <>
      <Backdrop depth={d} />
      <Seal r={r} turn={0} split={0} lit={0.1 + Math.sin(Math.PI * p) * 0.1} />
      <Line eyebrow="WELCOME BACK" head="DiaPony" o={settle * (1 - MOTION.enter(seg(p, 0.66, 0.94)))} rise={mix(20, 0, settle)} />
      <div style={{
        position: 'absolute', left: 0, right: 0, bottom: 92,
        display: 'flex', justifyContent: 'center',
        opacity: MOTION.enter(seg(p, 0.3, 0.62)) * (1 - MOTION.enter(seg(p, 0.7, 0.96))),
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 11, padding: '10px 20px', background: 'rgba(0,0,0,.45)', border: `1px solid rgba(200,164,92,.32)` }}>
          <i style={{ width: 7, height: 7, display: 'block', borderRadius: '50%', background: P.good }} />
          <span style={{ font: `400 14px/1 'Spectral',serif`, color: P.dim }}>Seal verified · 2 341 duelists inside</span>
        </div>
      </div>
    </>
  );
}

function SceneUnlock() {
  const { progress: p } = useScene();
  const d = mix(0.18, 0.48, MOTION.drift(p));
  const r = mix(172, 210, MOTION.drift(p));
  const turn = MOTION.enter(p) * 300;
  const tumblers = seg(p, 0.12, 0.86);
  // the two halves only begin to part in the last third
  const split = MOTION.enter(seg(p, 0.66, 1)) * 0.55;
  const lit = 0.2 + tumblers * 0.4 + split * 0.5;
  return (
    <>
      <Backdrop depth={d} glow={tumblers * 0.4} />
      <Seal r={r} turn={turn} split={split} lit={lit} tumblers={tumblers} />
      <Line eyebrow="UNSEALING" head="Six locks" o={MOTION.enter(seg(p, 0.04, 0.3)) * (1 - MOTION.enter(seg(p, 0.72, 1)))} rise={0} />
    </>
  );
}

function SceneOpen() {
  const { progress: p } = useScene();
  const rush = p * p;                       // ease-in: zero slope at the seam
  const d = mix(0.48, 1, rush);
  const r = mix(210, 340, rush);
  const split = mix(0.55, 1, rush / Math.max(0.72 * 0.72, 0.0001) > 1 ? 1 : (rush / (0.72 * 0.72)));
  const flare = Math.sin(Math.PI * seg(p, 0.1, 0.7));
  return (
    <>
      <Backdrop depth={d} glow={0.4 + flare * 0.6} />
      <Seal r={r} turn={mix(300, 460, MOTION.drift(p))} split={split} lit={0.7 + flare * 0.3} tumblers={1} />
      <div style={{
        position: 'absolute', left: CX, top: CY,
        width: 200 + flare * 1500, height: 200 + flare * 1500,
        transform: 'translate(-50%,-50%)', borderRadius: '50%',
        background: `radial-gradient(circle at 50% 50%, rgba(248,238,214,${flare * 0.5}), rgba(235,206,138,${flare * 0.16}) 34%, transparent 62%)`,
      }} />
      <div style={{ position: 'absolute', inset: 0, background: '#F8EED6', opacity: MOTION.enter(seg(p, 0.76, 1)) * 0.9 }} />
    </>
  );
}

function SceneArrive() {
  const { progress: p } = useScene();
  const wash = 1 - MOTION.enter(seg(p, 0, 0.36));
  const inn = MOTION.enter(seg(p, 0.2, 0.56));
  const out = 1 - MOTION.enter(seg(p, 0.9, 1));
  return (
    <>
      <Backdrop depth={mix(1, 0.04, MOTION.enter(seg(p, 0, 0.5)))} glow={wash * 0.5} />
      <div style={{
        position: 'absolute', left: 0, right: 0, top: 148,
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 26,
        opacity: inn * out, transform: `translateY(${mix(28, 0, inn)}px) scale(${mix(1.06, 1, inn)})`,
      }}>
        <div style={{ position: 'relative', width: 120, height: 120 }}>
          <div style={{ position: 'absolute', left: '50%', top: '50%', width: 120, height: 120, transform: 'translate(-50%,-50%) rotate(45deg)', border: `3px solid ${P.gold}` }} />
          <div style={{ position: 'absolute', left: '50%', top: '50%', width: 66, height: 66, transform: 'translate(-50%,-50%) rotate(45deg)', border: `2px solid ${P.light}`, background: 'linear-gradient(135deg,rgba(200,164,92,.3),rgba(200,164,92,.04))' }} />
          <div style={{ position: 'absolute', left: '50%', top: '50%', width: 28, height: 28, transform: 'translate(-50%,-50%) rotate(45deg)', background: `linear-gradient(135deg,${P.pale},${P.dark})`, boxShadow: '0 0 30px rgba(235,206,138,.65)' }} />
        </div>
        <span style={{
          font: `700 78px/1.2 'Cinzel',serif`, letterSpacing: '.09em',
          background: `linear-gradient(90deg,#A6802F 0%,${P.pale} 20%,${P.gold} 42%,${P.pale} 62%,#A6802F 100%)`,
          WebkitBackgroundClip: 'text', backgroundClip: 'text', color: 'transparent',
          whiteSpace: 'nowrap',
        }}>RELIQUARY</span>
        <span style={{ font: `400 20px/1 'Spectral',serif`, color: P.muted }}>Your vault holds 218 cards and 4 decks.</span>
      </div>
      <div style={{ position: 'absolute', inset: 0, background: '#F8EED6', opacity: wash * 0.9 }} />
    </>
  );
}

function VaultEnter() {
  return (
    <SceneStage width={W} height={H} scenes={window.OM_SCENES} playback={window.OM_PLAYBACK} bg="#0A0705">
      {{ Approach: SceneApproach, Unlock: SceneUnlock, Open: SceneOpen, Arrive: SceneArrive }}
    </SceneStage>
  );
}

window.VaultEnter = VaultEnter;
