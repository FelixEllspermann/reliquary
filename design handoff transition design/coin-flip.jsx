// coin-flip.jsx — RELIQUARY coin toss. Scene components for animations-v2.
// The coin's flight is expressed as two continuous quantities, altitude `h`
// (0 = table, 1 = apex) and `spin` (turns), whose values are pinned at every
// scene boundary so each cut lands on the settled composition.

const { SceneStage, useScene } = window;

/* ---------------- palette (Reliquary) ---------------- */
const P = {
  gold: '#C8A45C', light: '#EBCE8A', pale: '#F8EED6',
  dark: '#7A5A1E', deep: '#3B2A10', ink: '#2E2417',
  parch1: '#EBE1C7', parch2: '#D9CCAB',
  ember: '#E0603A', teal: '#8FC6D2', good: '#7ACD96',
  ag: '#A9B2BE', agLight: '#D6DDE6', agPale: '#F2F5F8', agDark: '#5A6472', agDeep: '#262C34',
  muted: '#A2917A', dim: '#9C8A6A',
};

/* ---------------- math ---------------- */
const c01 = (v) => (v < 0 ? 0 : v > 1 ? 1 : v);
// remap p from [a,b] onto [0,1]
const seg = (p, a, b) => c01((p - a) / (b - a));
const mix = (a, b, t) => a + (b - a) * t;

/* ---------------- the three motion helpers ---------------- */
const MOTION = {
  enter: (p) => 1 - Math.pow(1 - c01(p), 3),                 // ease-out cubic
  drift: (p) => 0.5 - 0.5 * Math.cos(Math.PI * c01(p)),      // ease-in-out sine
  pop: (p) => { const t = c01(p), s = 1.9; return 1 + (s + 1) * Math.pow(t - 1, 3) + s * Math.pow(t - 1, 2); },
};

/* ---------------- stage geometry ---------------- */
const W = 1280, H = 720;
const GROUND = 508;          // table line, px from top
const RISE = 336;            // apex travel in px
const D = 168;               // coin diameter
const CFG = { turns: 8, winner: 'relic' };
// integer turns land cos(theta) = +1 (RELIC up); +0.5 lands cos = -1 (SEAL up)
const landTurns = () => CFG.turns + (CFG.winner === 'seal' ? 0.5 : 0);

/* ---------------- persistent stage ---------------- */
const EMBERS = [
  { x: 118, d: 0.00, s: 7, c: P.gold }, { x: 322, d: 0.35, s: 5, c: P.light },
  { x: 556, d: 0.62, s: 6, c: P.gold }, { x: 792, d: 0.18, s: 5, c: P.ember },
  { x: 988, d: 0.80, s: 6, c: P.light }, { x: 1186, d: 0.48, s: 7, c: P.gold },
];

function Table({ cam, embers = 0 }) {
  return (
    <div style={{
      position: 'absolute', inset: 0, overflow: 'hidden',
      transform: `scale(${cam.s}) translateY(${cam.y}px)`, transformOrigin: '50% 46%',
    }}>
      <div style={{ position: 'absolute', inset: -80, background: `radial-gradient(ellipse 1080px 620px at 50% 46%, #2A1C12, #0A0705 76%)` }} />
      <div style={{
        position: 'absolute', inset: -80,
        background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.045) 0 1px,transparent 1px 28px),repeating-linear-gradient(-45deg,rgba(200,164,92,.045) 0 1px,transparent 1px 28px)',
      }} />
      {/* table plane */}
      <div style={{
        position: 'absolute', left: -80, right: -80, top: GROUND, bottom: -80,
        background: `linear-gradient(180deg, rgba(96,52,18,.34), rgba(96,52,18,0) 70%)`,
        borderTop: `1px solid rgba(200,164,92,.22)`,
      }} />
      {/* two nested ornament diamonds, echoing the card back */}
      <div style={{
        position: 'absolute', left: W / 2, top: GROUND - 96, width: 520, height: 520,
        transform: 'translate(-50%,-50%) rotate(45deg)', border: `1px solid rgba(200,164,92,.10)`,
      }} />
      <div style={{
        position: 'absolute', left: W / 2, top: GROUND - 96, width: 320, height: 320,
        transform: 'translate(-50%,-50%) rotate(45deg)', border: `1px solid rgba(200,164,92,.07)`,
      }} />
      {EMBERS.map((e, i) => {
        const t = (embers + e.d) % 1;
        return (
          <div key={i} style={{
            position: 'absolute', left: e.x, top: GROUND + 40 - t * 330,
            width: e.s, height: e.s, background: e.c, transform: 'rotate(45deg)',
            opacity: (t < 0.18 ? t / 0.18 : 1 - (t - 0.18) / 0.82) * 0.5,
          }} />
        );
      })}
      <div style={{ position: 'absolute', inset: -80, boxShadow: 'inset 0 0 220px rgba(0,0,0,.88)' }} />
    </div>
  );
}

/* ---------------- the coin ---------------- */
function FaceRelic() {
  return (
    <div style={{
      position: 'absolute', inset: 0, borderRadius: '50%', overflow: 'hidden',
      background: `radial-gradient(circle at 34% 28%, ${P.pale}, ${P.gold} 46%, ${P.dark} 88%)`,
      boxShadow: `inset 0 0 0 6px ${P.light}, inset 0 0 0 8px ${P.deep}, inset 0 -10px 24px rgba(0,0,0,.45)`,
    }}>
      <div style={{ position: 'absolute', left: '50%', top: '50%', width: 96, height: 96, transform: 'translate(-50%,-50%) rotate(45deg)', border: `4px solid ${P.deep}` }} />
      <div style={{ position: 'absolute', left: '50%', top: '50%', width: 52, height: 52, transform: 'translate(-50%,-50%) rotate(45deg)', border: `3px solid ${P.deep}`, background: 'rgba(59,42,16,.18)' }} />
      <div style={{ position: 'absolute', left: '50%', top: '50%', width: 22, height: 22, transform: 'translate(-50%,-50%) rotate(45deg)', background: P.deep }} />
    </div>
  );
}

function FaceSeal() {
  return (
    <div style={{
      position: 'absolute', inset: 0, borderRadius: '50%', overflow: 'hidden',
      background: `radial-gradient(circle at 34% 28%, ${P.agPale}, ${P.ag} 50%, ${P.agDark} 90%)`,
      boxShadow: `inset 0 0 0 6px ${P.agLight}, inset 0 0 0 8px ${P.agDeep}, inset 0 -10px 24px rgba(0,0,0,.5)`,
    }}>
      <div style={{ position: 'absolute', left: '50%', top: '50%', width: 104, height: 104, transform: 'translate(-50%,-50%)', borderRadius: '50%', border: `4px solid ${P.agDeep}` }} />
      {[0, 45, 90, 135].map((a) => (
        <div key={a} style={{
          position: 'absolute', left: '50%', top: '50%', width: 128, height: 3,
          transform: `translate(-50%,-50%) rotate(${a}deg)`, background: 'rgba(38,44,52,.6)',
        }} />
      ))}
      <div style={{
        position: 'absolute', left: '50%', top: '50%', width: 46, height: 50,
        transform: 'translate(-50%,-50%)', background: P.agDeep,
        clipPath: 'polygon(50% 0,100% 20%,100% 66%,50% 100%,0 66%,0 20%)',
      }} />
    </div>
  );
}

function Coin({ h, spin, tilt = 0, glow = 0 }) {
  const theta = spin * Math.PI * 2;
  const c = Math.cos(theta);
  const sy = Math.abs(c);
  const front = c >= 0;
  const edge = sy < 0.1;
  const y = GROUND - h * RISE;
  const shadowW = mix(D * 1.02, D * 0.34, c01(h));
  const shadowO = mix(0.5, 0.06, c01(h));

  return (
    <>
      <div style={{
        position: 'absolute', left: W / 2, top: GROUND + 8,
        width: shadowW, height: shadowW * 0.2,
        transform: 'translate(-50%,-50%)', borderRadius: '50%',
        background: `radial-gradient(ellipse at 50% 50%, rgba(0,0,0,${shadowO}), transparent 72%)`,
      }} />
      <div style={{
        position: 'absolute', left: W / 2, top: y, width: D, height: D,
        transform: `translate(-50%,-50%) rotate(${tilt}deg) scaleY(${Math.max(sy, 0.02)})`,
        filter: glow > 0 ? `drop-shadow(0 0 ${18 + glow * 34}px rgba(235,206,138,${0.28 + glow * 0.42}))` : 'none',
      }}>
        {front ? <FaceRelic /> : <FaceSeal />}
        {edge && (
          <div style={{
            position: 'absolute', left: 0, right: 0, top: '50%', height: 10,
            transform: 'translateY(-50%) scaleY(' + (1 / Math.max(sy, 0.02)) + ')',
            borderRadius: 5,
            background: front
              ? `linear-gradient(180deg, ${P.pale}, ${P.gold} 45%, ${P.deep})`
              : `linear-gradient(180deg, ${P.agPale}, ${P.ag} 45%, ${P.agDeep})`,
            opacity: 1 - sy / 0.1,
          }} />
        )}
      </div>
    </>
  );
}

/* ---------------- caption / banner ---------------- */
function Caption({ text, o, rise }) {
  if (o <= 0.001) return null;
  return (
    <div style={{
      position: 'absolute', left: 0, right: 0, top: 92,
      display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 18,
      opacity: o, transform: `translateY(${rise}px)`,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
        <div style={{ width: 74, height: 1, background: `linear-gradient(90deg,transparent,${P.gold})` }} />
        <span style={{ font: `500 14px/1 'Oswald',sans-serif`, letterSpacing: '.38em', color: P.dim }}>THE TOSS</span>
        <div style={{ width: 74, height: 1, background: `linear-gradient(270deg,transparent,${P.gold})` }} />
      </div>
      <span style={{ font: `700 54px/1.2 'Cinzel',serif`, letterSpacing: '.05em', color: '#F1DFB8', whiteSpace: 'nowrap' }}>{text}</span>
    </div>
  );
}

function Choice({ label, numeral, note, o, rise, dim }) {
  return (
    <div style={{
      position: 'relative', width: 300, boxSizing: 'border-box',
      borderRadius: 9, border: `2px solid ${P.gold}`,
      background: 'linear-gradient(165deg,#3A2818,#140C07 58%,#291A0C)',
      boxShadow: '0 20px 44px rgba(0,0,0,.7)',
      opacity: o * (dim ? 0.42 : 1), transform: `translateY(${rise}px)`,
      display: 'flex', flexDirection: 'column',
    }}>
      <div style={{ position: 'absolute', inset: 6, border: `1px solid rgba(200,164,92,.32)`, borderRadius: 4, pointerEvents: 'none' }} />
      <div style={{
        position: 'absolute', right: -11, top: -13, width: 38, height: 42,
        background: `linear-gradient(160deg,${P.light},#8E6A22)`,
        clipPath: 'polygon(50% 0,100% 20%,100% 66%,50% 100%,0 66%,0 20%)',
        display: 'flex', alignItems: 'center', justifyContent: 'center',
      }}>
        <div style={{ position: 'absolute', inset: 2, background: `linear-gradient(160deg,${P.deep},#180F04)`, clipPath: 'polygon(50% 0,100% 20%,100% 66%,50% 100%,0 66%,0 20%)' }} />
        <span style={{ position: 'relative', font: `700 17px/1.2 'Cinzel',serif`, color: '#F3DDA4' }}>{numeral}</span>
      </div>
      <div style={{ padding: '22px 22px 16px', display: 'flex', flexDirection: 'column', gap: 8 }}>
        <span style={{ font: `700 30px/1.2 'Cinzel',serif`, letterSpacing: '.05em', color: '#F5EBD4', whiteSpace: 'nowrap' }}>{label}</span>
      </div>
      <div style={{
        margin: '0 8px 8px', padding: '11px 13px',
        background: `linear-gradient(180deg,${P.parch1},${P.parch2})`, border: '1px solid #8C7440',
      }}>
        <span style={{ font: `400 13px/1.4 'Spectral',serif`, color: P.ink }}>{note}</span>
      </div>
    </div>
  );
}

function Verdict({ o, rise, ringO, ringS, a1, a2, hint }) {
  const relic = CFG.winner === 'relic';
  const call = relic ? 'RELIC · YOU CALLED IT' : 'SEAL · LYRA CALLED IT';
  const head = relic ? 'YOUR CHOICE' : "LYRA'S CHOICE";
  const tone = relic ? P.good : P.teal;
  return (
    <>
      <div style={{
        position: 'absolute', left: W / 2, top: GROUND - 6,
        width: 300 * ringS, height: 300 * ringS * 0.26,
        transform: 'translate(-50%,-50%)', borderRadius: '50%',
        border: `2px solid rgba(235,206,138,${ringO})`,
      }} />

      <div style={{
        position: 'absolute', left: 0, right: 0, top: 84,
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 18,
        opacity: o, transform: `translateY(${rise}px)`,
      }}>
        <div style={{
          display: 'flex', alignItems: 'center', gap: 12, padding: '8px 18px',
          background: 'rgba(0,0,0,.45)', border: `1px solid ${tone}80`,
        }}>
          <i style={{ width: 9, height: 9, display: 'block', background: tone, transform: 'rotate(45deg)' }} />
          <span style={{ font: `500 13px/1 'Oswald',sans-serif`, letterSpacing: '.3em', color: tone }}>{call}</span>
        </div>
        <span style={{ font: `700 66px/1.2 'Cinzel',serif`, letterSpacing: '.06em', color: '#F8EED6', whiteSpace: 'nowrap', textShadow: '0 0 60px rgba(200,164,92,.3)' }}>{head}</span>
        <span style={{ font: `400 19px/1 'Spectral',serif`, color: P.muted }}>
          {relic ? 'Take the first turn, or hand it over.' : 'Lyra decides who opens.'}
        </span>
      </div>

      <div style={{ position: 'absolute', left: 96, top: 372 }}>
        <Choice label="GO FIRST" numeral="I" o={a1} rise={mix(24, 0, a1)} dim={!relic}
                note="You open the duel and set the pace — but you draw no card on turn one." />
      </div>
      <div style={{ position: 'absolute', left: 884, top: 372 }}>
        <Choice label="GO SECOND" numeral="II" o={a2} rise={mix(24, 0, a2)} dim={!relic}
                note="Lyra opens and shows her hand first — you draw one extra card." />
      </div>

      <div style={{
        position: 'absolute', left: 0, right: 0, bottom: 44,
        display: 'flex', justifyContent: 'center', opacity: hint,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 11, padding: '10px 18px', background: 'rgba(0,0,0,.45)', border: `1px solid rgba(200,164,92,.3)` }}>
          <i style={{ width: 7, height: 7, display: 'block', borderRadius: '50%', background: relic ? P.gold : P.teal }} />
          <span style={{ font: `400 14px/1 'Spectral',serif`, color: P.dim }}>
            {relic ? 'Choose within 15 s — GO FIRST is taken by default.' : 'Waiting for Lyra to choose…'}
          </span>
        </div>
      </div>
    </>
  );
}

/* ================= SCENES ================= */
/* Boundary contract
   ┌──────────┬──────────┬──────────┐
   │ scene    │ h in→out │ spin     │
   ├──────────┼──────────┼──────────┤
   │ Toss     │ 0  → .62 │ 0  → 3.2 │
   │ Apex     │ .62→ .62 │ 3.2→ 6.0 │  (peaks at 1.0 mid-scene)
   │ Land     │ .62→ 0   │ 6.0→ 8.0 │
   │ Verdict  │ 0  → 0   │ 8.0      │
   └──────────┴──────────┴──────────┘ */

function SceneToss() {
  const { progress: p } = useScene();
  // anticipation: a squat that ramps in and back out inside (0, .26)
  const a = Math.sin(Math.PI * seg(p, 0, 0.26));
  const fly = seg(p, 0.26, 1);
  const h = MOTION.enter(fly) * 0.62 - a * 0.03;
  const spin = MOTION.enter(fly) * 3.2;
  const cam = { s: mix(1.0, 1.06, MOTION.drift(p)), y: mix(0, -20, MOTION.drift(p)) };
  return (
    <>
      <Table cam={cam} embers={p * 0.3} />
      <Coin h={h} spin={spin} tilt={a * -3} />
      <Caption text="Who goes first?" o={seg(p, 0.34, 0.74)} rise={mix(18, 0, MOTION.enter(seg(p, 0.34, 0.74)))} />
    </>
  );
}

function SceneApex() {
  const { progress: p } = useScene();
  const arc = Math.sin(Math.PI * p);                 // 0 → 1 → 0
  const h = mix(0.62, 1.0, arc);
  const spin = mix(3.2, 6.0, p);
  const cam = { s: mix(1.06, 1.12, MOTION.drift(p)), y: mix(-20, -34, MOTION.drift(p)) };
  const out = 1 - MOTION.enter(seg(p, 0.6, 0.92));
  return (
    <>
      <Table cam={cam} embers={0.3 + p * 0.34} />
      <Coin h={h} spin={spin} />
      <Caption text="Who goes first?" o={out} rise={mix(0, -14, 1 - out)} />
    </>
  );
}

function SceneLand() {
  const { progress: p } = useScene();
  const fall = seg(p, 0, 0.52);
  const drop = 0.62 * (1 - fall * fall);             // accelerating
  // one small bounce, then a rocking settle — both back to 0 before p = 1
  const bounce = Math.sin(Math.PI * seg(p, 0.52, 0.74)) * 0.075;
  const settle = Math.sin(Math.PI * seg(p, 0.74, 1)) * 0.012;
  const h = drop + bounce + settle;
  const spin = mix(6.0, landTurns(), MOTION.enter(seg(p, 0, 0.62)));
  // sin (not cos) so the rock is 0 at both progress 0 and progress 1
  const rock = Math.sin(seg(p, 0.52, 1) * Math.PI * 5) * (1 - MOTION.enter(seg(p, 0.52, 1))) * 7;
  const cam = { s: mix(1.12, 1.0, MOTION.drift(p)), y: mix(-34, 0, MOTION.drift(p)) };
  const dust = seg(p, 0.5, 0.9);
  return (
    <>
      <Table cam={cam} embers={0.64 + p * 0.26} />
      {dust > 0 && (
        <div style={{
          position: 'absolute', left: W / 2, top: GROUND + 4,
          width: mix(120, 420, MOTION.enter(dust)), height: mix(120, 420, MOTION.enter(dust)) * 0.24,
          transform: 'translate(-50%,-50%)', borderRadius: '50%',
          border: `2px solid rgba(200,164,92,${(1 - dust) * 0.5})`,
        }} />
      )}
      <Coin h={h} spin={spin} tilt={rock} glow={MOTION.enter(seg(p, 0.52, 0.8)) * (1 - seg(p, 0.8, 1)) * 0.5} />
    </>
  );
}

function SceneVerdict() {
  const { progress: p } = useScene();
  const inn = MOTION.enter(seg(p, 0.06, 0.4));
  const out = 1 - MOTION.enter(seg(p, 0.88, 1));
  const ring = seg(p, 0.02, 0.4);
  const cam = { s: 1 + Math.sin(Math.PI * p) * 0.05, y: 0 };
  return (
    <>
      <Table cam={cam} embers={0.9 + p * 0.3} />
      <Coin h={0} spin={landTurns()} glow={(0.45 + Math.sin(Math.PI * p * 2) * 0.25) * out} />
      <Verdict
        o={inn * out}
        rise={mix(26, 0, inn)}
        ringO={(1 - ring) * 0.55 * out}
        ringS={mix(0.8, 1.9, MOTION.pop(ring) * 0.55 + ring * 0.45)}
        a1={MOTION.enter(seg(p, 0.30, 0.56)) * out}
        a2={MOTION.enter(seg(p, 0.38, 0.64)) * out}
        hint={MOTION.enter(seg(p, 0.58, 0.78)) * out}
      />
    </>
  );
}

/* ================= root ================= */
function CoinFlip() {
  const { useTweaks, TweaksPanel, TweakSection, TweakSlider, TweakRadio, TweakToggle } = window;
  const [t, setTweak] = useTweaks(window.TWEAK_DEFAULTS);
  CFG.turns = t.turns;
  CFG.winner = t.winner;
  return (
    <>
    <SceneStage
      width={W}
      height={H}
      scenes={window.OM_SCENES}
      playback={window.OM_PLAYBACK}
      bg="#0A0705"
    >
      {{ Toss: SceneToss, Apex: SceneApex, Land: SceneLand, Verdict: SceneVerdict }}
    </SceneStage>
    <TweaksPanel>
      <TweakSection label="Outcome" />
      <TweakRadio label="Winning face" value={t.winner} options={['relic', 'seal']}
                  onChange={(v) => setTweak('winner', v)} />
      <TweakSection label="Motion" />
      <TweakSlider label="Spin" value={t.turns} min={5} max={14} step={1} unit=" turns"
                   onChange={(v) => setTweak('turns', v)} />
      <TweakSection label="Editing" />
      <TweakToggle label="Motion editor" value={t.motionEditor}
                   onChange={(v) => setTweak('motionEditor', v)} />
    </TweaksPanel>
    </>
  );
}

window.CoinFlip = CoinFlip;
