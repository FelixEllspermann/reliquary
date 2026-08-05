// duel-load.jsx — RELIQUARY: coin choice → board. Scenes for animations-v2.
// A shuffle-tumble: the deck gathers, tumbles as one mass, then flings out
// into the five monster zones and the board frame resolves underneath.

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

/* fixed per-card constants — deterministic, never Math.random() */
const CARDS = [
  { a0: -34, a1: -13, n: 2, fl: 3, dx: -420, dy: 118, d: 0.00 },
  { a0: -17, a1: -6.5, n: 3, fl: 2, dx: -212, dy: 118, d: 0.06 },
  { a0: 4, a1: 0, n: 2, fl: 4, dx: 0, dy: 118, d: 0.12 },
  { a0: 21, a1: 6.5, n: 3, fl: 3, dx: 212, dy: 118, d: 0.18 },
  { a0: 38, a1: 13, n: 2, fl: 2, dx: 420, dy: 118, d: 0.24 },
  { a0: -26, a1: 0, n: 3, fl: 4, dx: -108, dy: -126, d: 0.30 },
  { a0: 29, a1: 0, n: 2, fl: 3, dx: 108, dy: -126, d: 0.36 },
];

/* ---------------- board that resolves underneath ---------------- */
function Board({ o, tint }) {
  if (o <= 0.001) return null;
  const zone = (x, y, c, w = 92, h = 129) => (
    <div key={`${x}-${y}`} style={{
      position: 'absolute', left: x, top: y, width: w, height: h,
      boxSizing: 'border-box', borderRadius: 4,
      border: `1px dashed ${c}`, background: 'rgba(0,0,0,.3)',
    }} />
  );
  const g = 'rgba(200,164,92,.4)', t = 'rgba(143,198,210,.42)', v = 'rgba(185,163,224,.42)';
  return (
    <div style={{ position: 'absolute', inset: 0, opacity: o }}>
      {/* opponent half — cool */}
      <div style={{ position: 'absolute', left: 0, right: 0, top: 0, height: 300, background: 'linear-gradient(180deg,rgba(40,62,86,.42),transparent)' }} />
      <div style={{ position: 'absolute', left: 0, right: 0, bottom: 0, height: 280, background: `linear-gradient(0deg,rgba(96,52,18,${0.3 * tint}),transparent)` }} />
      <div style={{ position: 'absolute', left: 120, right: 120, top: CY - 1, height: 1, background: `linear-gradient(90deg,transparent,rgba(200,164,92,.5),transparent)` }} />
      {[0, 1, 2, 3, 4].map((i) => zone(CX - 260 + i * 104, 118, i < 3 ? t : v))}
      {[0, 1, 2, 3, 4].map((i) => zone(CX - 260 + i * 104, 256, g))}
      {[0, 1, 2, 3, 4].map((i) => zone(CX - 260 + i * 104, 396, g))}
      {[0, 1, 2, 3, 4].map((i) => zone(CX - 260 + i * 104, 534, i < 2 ? t : v))}
    </div>
  );
}

/* ---------------- a card back ---------------- */
function CardBack({ x, y, rot, spin, scale, o, lit = 0 }) {
  const c = Math.cos(spin * Math.PI * 2);
  const w = 116, h = 162;
  return (
    <div style={{
      position: 'absolute', left: CX + x, top: CY + y,
      width: w, height: h, opacity: o,
      transform: `translate(-50%,-50%) rotate(${rot}deg) scale(${scale}) scaleX(${Math.max(Math.abs(c), 0.06)})`,
      borderRadius: 7, overflow: 'hidden',
      border: `2px solid ${c >= 0 ? P.gold : P.light}`,
      background: `radial-gradient(ellipse at 50% 50%, #4E2A18, #1C0E08 78%)`,
      boxShadow: `0 18px 40px rgba(0,0,0,.75)${lit > 0 ? `, 0 0 ${18 + lit * 30}px rgba(235,206,138,${lit * 0.5})` : ''}`,
    }}>
      <div style={{
        position: 'absolute', inset: 0,
        background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.15) 0 1px,transparent 1px 10px),repeating-linear-gradient(-45deg,rgba(200,164,92,.15) 0 1px,transparent 1px 10px)',
      }} />
      <div style={{ position: 'absolute', inset: 6, border: `1px solid rgba(200,164,92,.45)`, borderRadius: 3 }} />
      <div style={{ position: 'absolute', left: '50%', top: '50%', width: 58, height: 58, transform: 'translate(-50%,-50%) rotate(45deg)', border: `1px solid rgba(200,164,92,.55)` }} />
      <div style={{ position: 'absolute', left: '50%', top: '50%', width: 24, height: 24, transform: 'translate(-50%,-50%) rotate(45deg)', background: `linear-gradient(135deg,${P.light},${P.dark})` }} />
    </div>
  );
}

function Caption({ text, sub, o, rise }) {
  if (o <= 0.001) return null;
  return (
    <div style={{
      position: 'absolute', left: 0, right: 0, top: 74,
      display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 14,
      opacity: o, transform: `translateY(${rise}px)`,
    }}>
      <span style={{ font: `700 46px/1.2 'Cinzel',serif`, letterSpacing: '.05em', color: '#F1DFB8', whiteSpace: 'nowrap' }}>{text}</span>
      {sub && <span style={{ font: `400 17px/1 'Spectral',serif`, color: P.muted, whiteSpace: 'nowrap' }}>{sub}</span>}
    </div>
  );
}

/* ================= SCENES =================
   gather 0→1 | tumble spin 0→3 | deal 0→1 | board o 0→1                  */

function SceneGather() {
  const { progress: p } = useScene();
  const g = MOTION.enter(p);
  return (
    <>
      <div style={{ position: 'absolute', inset: 0, background: `radial-gradient(ellipse 1000px 640px at 50% 48%, #2A1C12, #0A0705 76%)` }} />
      <div style={{ position: 'absolute', inset: 0, background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.045) 0 1px,transparent 1px 28px),repeating-linear-gradient(-45deg,rgba(200,164,92,.045) 0 1px,transparent 1px 28px)' }} />
      {CARDS.map((c, i) => {
        const t = MOTION.enter(seg(p, c.d * 0.5, 0.5 + c.d * 0.5));
        return (
          <CardBack key={i}
            x={mix(c.dx * 1.9, 0, t)} y={mix(c.dy * 2.4 + 300, 0, t)}
            rot={mix(c.a0 * 2.4, c.a0 * 0.35, t)} spin={0} scale={mix(0.8, 1, t)} o={t} />
        );
      })}
      <Caption text="Shuffling" sub="40 cards · Dragons" o={MOTION.enter(seg(p, 0.24, 0.6))} rise={mix(18, 0, MOTION.enter(seg(p, 0.24, 0.6)))} />
      <div style={{ position: 'absolute', inset: 0, boxShadow: 'inset 0 0 220px rgba(0,0,0,.88)' }} />
    </>
  );
}

function SceneTumble() {
  const { progress: p } = useScene();
  const swirl = MOTION.drift(p);
  const wob = Math.sin(Math.PI * p * 2) * 26;
  return (
    <>
      <div style={{ position: 'absolute', inset: 0, background: `radial-gradient(ellipse 1000px 640px at 50% 48%, #2A1C12, #0A0705 76%)` }} />
      <div style={{
        position: 'absolute', inset: 0, transform: `rotate(${swirl * 22}deg)`, transformOrigin: '50% 50%',
        background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.05) 0 1px,transparent 1px 28px),repeating-linear-gradient(-45deg,rgba(200,164,92,.05) 0 1px,transparent 1px 28px)',
      }} />
      <div style={{
        position: 'absolute', left: CX, top: CY, width: mix(380, 620, swirl), height: mix(380, 620, swirl),
        transform: `translate(-50%,-50%) rotate(${45 + swirl * 90}deg)`,
        border: `1px solid rgba(200,164,92,${0.2 - swirl * 0.1})`,
      }} />
      {CARDS.map((c, i) => {
        const a = c.a0 * 0.35 + swirl * 360 * c.n;
        const rad = Math.sin(Math.PI * p) * 96;
        return (
          <CardBack key={i}
            x={Math.cos((a * Math.PI) / 180) * rad}
            y={Math.sin((a * Math.PI) / 180) * rad * 0.5 + wob * 0.2}
            rot={a} spin={p * c.fl} scale={mix(1, 1.06, Math.sin(Math.PI * p))} o={1}
            lit={Math.sin(Math.PI * p) * 0.35} />
        );
      })}
      <Caption text="Shuffling" sub="40 cards · Dragons" o={1 - MOTION.enter(seg(p, 0.6, 0.94))} rise={mix(0, -12, MOTION.enter(seg(p, 0.6, 0.94)))} />
      <div style={{ position: 'absolute', inset: 0, boxShadow: 'inset 0 0 220px rgba(0,0,0,.88)' }} />
    </>
  );
}

function SceneDeal() {
  const { progress: p } = useScene();
  const board = MOTION.enter(seg(p, 0.3, 0.9));
  return (
    <>
      <div style={{ position: 'absolute', inset: 0, background: `radial-gradient(ellipse 1000px 640px at 50% 48%, #241811, #0B0705 76%)` }} />
      <div style={{ position: 'absolute', inset: 0, background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.045) 0 1px,transparent 1px 26px),repeating-linear-gradient(-45deg,rgba(200,164,92,.045) 0 1px,transparent 1px 26px)' }} />
      <Board o={board} tint={board} />
      {CARDS.map((c, i) => {
        const t = MOTION.enter(seg(p, c.d * 0.9, 0.52 + c.d * 0.9));
        const arc = Math.sin(Math.PI * t) * -54;
        return (
          <CardBack key={i}
            x={mix(0, c.dx, t)} y={mix(0, c.dy, t) + arc}
            rot={mix(c.a0 * 0.35, c.a1, t)}
            spin={0}
            scale={mix(1, 0.79, t)} o={1}
            lit={(1 - t) * 0.3} />
        );
      })}
      <div style={{ position: 'absolute', inset: 0, boxShadow: 'inset 0 0 220px rgba(0,0,0,.86)' }} />
    </>
  );
}

function SceneSettle() {
  const { progress: p } = useScene();
  const fade = 1 - MOTION.enter(seg(p, 0.1, 0.62));
  const inn = MOTION.enter(seg(p, 0.16, 0.5));
  const out = 1 - MOTION.enter(seg(p, 0.9, 1));
  return (
    <>
      <div style={{ position: 'absolute', inset: 0, background: `radial-gradient(ellipse 1000px 640px at 50% 48%, #241811, #0B0705 76%)` }} />
      <div style={{ position: 'absolute', inset: 0, background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.045) 0 1px,transparent 1px 26px),repeating-linear-gradient(-45deg,rgba(200,164,92,.045) 0 1px,transparent 1px 26px)' }} />
      <Board o={1} tint={1} />
      {CARDS.map((c, i) => (
        <CardBack key={i} x={c.dx} y={c.dy} rot={mix(c.a1, c.a1 * 0.3, MOTION.enter(p))}
          spin={0} scale={mix(0.79, 0.76, MOTION.enter(p))} o={fade} />
      ))}
      <div style={{
        position: 'absolute', left: 0, right: 0, top: CY - 62,
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 18,
        opacity: inn * out, transform: `translateY(${mix(20, 0, inn)}px)`,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
          <i style={{ width: 10, height: 10, display: 'block', background: P.gold, transform: 'rotate(45deg)' }} />
          <span style={{ font: `700 56px/1.2 'Cinzel',serif`, letterSpacing: '.06em', color: '#F8EED6', whiteSpace: 'nowrap', textShadow: '0 0 50px rgba(200,164,92,.3)' }}>YOUR TURN</span>
          <i style={{ width: 10, height: 10, display: 'block', background: P.gold, transform: 'rotate(45deg)' }} />
        </div>
        <div style={{ padding: '12px 20px', background: `linear-gradient(180deg,${P.parch1},${P.parch2})`, border: '1px solid #8C7440' }}>
          <span style={{ font: `400 16px/1 'Spectral',serif`, color: P.ink }}>Draw phase skipped — you chose to go first.</span>
        </div>
      </div>
      <div style={{ position: 'absolute', inset: 0, boxShadow: 'inset 0 0 220px rgba(0,0,0,.86)' }} />
    </>
  );
}

function DuelLoad() {
  return (
    <SceneStage width={W} height={H} scenes={window.OM_SCENES} playback={window.OM_PLAYBACK} bg="#0B0705">
      {{ Gather: SceneGather, Tumble: SceneTumble, Deal: SceneDeal, Settle: SceneSettle }}
    </SceneStage>
  );
}

window.DuelLoad = DuelLoad;
