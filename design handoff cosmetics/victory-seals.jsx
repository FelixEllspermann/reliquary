// victory-seals.jsx — RELIQUARY victory seal stamps.
// A victory seal is the mark that lands on the win screen when you take a duel.
// Both players see it, so each one has to read in about a second and a half and
// end on a still frame worth holding. The five differ in HOW they arrive:
// burned, fractured, expanded, struck, occluded.

const { SceneStage, useScene } = window;

const P = {
  gold: '#C8A45C', light: '#EBCE8A', pale: '#F8EED6', dark: '#7A5A1E', deep: '#3B2A10',
  teal: '#8FC6D2', tealLit: '#DFF4F8', violet: '#B9A3E0', violetLit: '#EFE7FA',
  ember: '#E0603A', emberLit: '#F3C3A6', brand: '#7E4A20', brandLit: '#C8894E',
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
// The band left free by the win-screen text runs y 169 (under the headline) to
// y 526 (above the chip row), so its centre is 348 and its half-height is 178.
// A 45deg-rotated diamond of side r has a bounding box of r*sqrt(2), i.e. a
// half-height of 0.707r — so r must stay under 250. Bloom's widest ring (0.987r)
// is the real limit: 0.698r <= 178 gives r <= 254. 236 leaves a margin.
const CX = W / 2, CY = 348;
const R_FULL = 236;

const cfg = { reel: false, banner: true };

/* ---------------- shared stage ---------------- */
function Stage({ tone = P.gold, lift = 0, flash = 0 }) {
  return (
    <div style={{ position: 'absolute', inset: 0, overflow: 'hidden' }}>
      <div style={{
        position: 'absolute', inset: -40,
        background: `radial-gradient(ellipse 960px 600px at 50% 46%, ${hexA(tone, 0.13 + lift * 0.1)}, #0A0705 76%)`,
      }} />
      <div style={{
        position: 'absolute', inset: 0,
        background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.04) 0 1px,transparent 1px 26px),repeating-linear-gradient(-45deg,rgba(200,164,92,.04) 0 1px,transparent 1px 26px)',
      }} />
      <div style={{ position: 'absolute', inset: 0, boxShadow: 'inset 0 0 200px rgba(0,0,0,.9)' }} />
      {flash > 0.002 && <div style={{ position: 'absolute', inset: 0, background: P.pale, opacity: flash * 0.3 }} />}
    </div>
  );
}

/* ================= THE FIVE SEALS =================
   Each takes its own progress `t` (0 = nothing, 1 = the catalogue still frame)
   and a radius `r`, so the same component serves the full-screen stamp and the
   comparison reel with no second implementation. */

/* 1 · BRAND — common. Burns in. Nothing moves; the heat does the work:
   a scorch spreads, the mark chars, the rim cools from white to ember to iron. */
function Brand({ r, t }) {
  const spread = MOTION.enter(seg(t, 0, 0.44));
  const heat = Math.sin(Math.PI * seg(t, 0.06, 0.72));
  const cool = MOTION.enter(seg(t, 0.5, 1));
  const rimC = cool < 0.5
    ? mix(1, 0, cool * 2) > 0.5 ? P.pale : P.emberLit
    : P.brand;
  return (
    <div style={{ position: 'absolute', left: CX, top: CY, width: 0, height: 0 }}>
      <div style={{
        position: 'absolute', left: -r * 0.72 * spread, top: -r * 0.72 * spread,
        width: r * 1.44 * spread, height: r * 1.44 * spread, borderRadius: '50%',
        background: `radial-gradient(circle at 50% 50%, ${hexA(P.brand, 0.6 * (1 - cool * 0.4))}, transparent 72%)`,
      }} />
      <div style={{
        position: 'absolute', left: -r * 0.5, top: -r * 0.5, width: r, height: r,
        boxSizing: 'border-box',
        transform: `rotate(45deg) scale(${mix(0.7, 1, MOTION.enter(seg(t, 0.04, 0.5)))})`,
        background: '#1A0F06',
        border: `${r * 0.056}px solid ${rimC}`,
        boxShadow: `0 0 ${r * (0.1 + heat * 0.4)}px ${hexA(P.emberLit, heat * 0.85)}`,
        opacity: MOTION.enter(seg(t, 0.02, 0.3)),
      }} />
      <div style={{
        position: 'absolute', left: -r * 0.193, top: -r * 0.193, width: r * 0.386, height: r * 0.386,
        transform: 'rotate(45deg)',
        background: cool > 0.6 ? P.brandLit : P.emberLit,
        opacity: MOTION.enter(seg(t, 0.16, 0.46)),
      }} />
      {[[-0.30, 0.055], [0.30, 0.045], [-0.14, 0.04], [0.14, 0.05]].map(([dx, s], i) => {
        const ph = seg(t, 0.2 + i * 0.08, 0.9);
        const sz = r * s;
        return (
          <div key={i} style={{
            position: 'absolute',
            left: r * dx - sz / 2,
            top: r * 0.28 - ph * r * 0.38 - sz / 2,
            width: sz, height: sz, transform: 'rotate(45deg)',
            background: P.brandLit, opacity: Math.sin(Math.PI * ph) * 0.7,
          }} />
        );
      })}
    </div>
  );
}

/* 2 · SHATTER — rare. The seal lands whole, then three fractures split it and
   the halves part just enough to read as broken. */
function Shatter({ r, t }) {
  const land = MOTION.pop(seg(t, 0, 0.3));
  const cracks = [
    { a: 28, w: 1.56, d: 0.30 },
    { a: -52, w: 1.56, d: 0.40 },
    { a: 78, w: 1.25, d: 0.52 },
  ];
  const part = MOTION.enter(seg(t, 0.46, 1)) * r * 0.045;
  return (
    <div style={{ position: 'absolute', left: CX, top: CY, width: 0, height: 0 }}>
      <div style={{
        position: 'absolute', left: -r * 0.5, top: -r * 0.5, width: r, height: r,
        boxSizing: 'border-box',
        transform: `rotate(45deg) scale(${mix(1.5, 1, c01(land))})`,
        border: `${r * 0.031}px solid ${P.teal}`,
        boxShadow: `0 0 ${r * 0.19}px ${hexA(P.teal, 0.5)}`,
        opacity: c01(seg(t, 0, 0.14)),
      }} />
      {cracks.map((c, i) => {
        const g = MOTION.enter(seg(t, c.d, c.d + 0.3));
        return (
          <div key={i} style={{
            position: 'absolute', left: -r * c.w * 0.5, top: -1,
            width: r * c.w * g, height: r * (i === 2 ? 0.0156 : 0.021),
            transform: `rotate(${c.a}deg) translate(${part * (i % 2 ? -1 : 1)}px,${part * (i % 2 ? 1 : -1)}px)`,
            transformOrigin: '50% 50%',
            background: `linear-gradient(90deg,transparent,${P.tealLit} 42%,${i === 2 ? '#B9E6F0' : P.tealLit} 62%,transparent)`,
            boxShadow: `0 0 ${r * 0.1}px ${hexA(P.tealLit, 0.9 * g)}`,
          }} />
        );
      })}
      <div style={{
        position: 'absolute', left: -r * 0.104, top: -r * 0.104, width: r * 0.208, height: r * 0.208,
        transform: `rotate(45deg) scale(${mix(0.4, 1, MOTION.pop(seg(t, 0.28, 0.62)))})`,
        background: P.tealLit,
        boxShadow: `0 0 ${r * 0.167}px ${hexA(P.tealLit, 1)}`,
      }} />
      {[0, 1, 2, 3, 4, 5].map((i) => {
        const ph = seg(t, 0.42 + (i % 3) * 0.06, 1);
        const a = (i * 61) * Math.PI / 180;
        const d = r * (0.34 + ph * 0.34);
        return (
          <div key={'s' + i} style={{
            position: 'absolute', left: Math.cos(a) * d, top: Math.sin(a) * d,
            width: r * 0.035, height: r * 0.035, transform: 'rotate(45deg)',
            background: P.tealLit, opacity: Math.sin(Math.PI * ph) * 0.8,
          }} />
        );
      })}
    </div>
  );
}

/* 3 · BLOOM — epic. Four diamonds expand outward from the core on a stagger and
   settle; the quiet one, no impact anywhere in it. */
function Bloom({ r, t }) {
  const rings = [
    { s: 0.267, d: 0.00, o: 0.95, w: 0.0133 },
    { s: 0.507, d: 0.12, o: 0.62, w: 0.0133 },
    { s: 0.747, d: 0.24, o: 0.36, w: 0.010 },
    { s: 0.987, d: 0.36, o: 0.16, w: 0.0067 },
  ];
  return (
    <div style={{ position: 'absolute', left: CX, top: CY, width: 0, height: 0 }}>
      {rings.map((g, i) => {
        const e = MOTION.drift(seg(t, g.d, g.d + 0.5));
        const sz = r * g.s * mix(0.3, 1, e);
        return (
          <div key={i} style={{
            position: 'absolute', left: -sz / 2, top: -sz / 2, width: sz, height: sz,
            boxSizing: 'border-box', transform: 'rotate(45deg)',
            border: `${Math.max(1, r * g.w)}px solid ${hexA(P.violetLit, g.o * e)}`,
          }} />
        );
      })}
      <div style={{
        position: 'absolute', left: -r * 0.12, top: -r * 0.12, width: r * 0.24, height: r * 0.24,
        transform: `rotate(45deg) scale(${mix(0.2, 1, MOTION.drift(seg(t, 0, 0.4)))})`,
        background: P.violetLit,
        boxShadow: `0 0 ${r * 0.23}px ${hexA(P.violet, 0.9)}`,
      }} />
      <div style={{
        position: 'absolute', left: -r * 0.6, top: -r * 0.6, width: r * 1.2, height: r * 1.2,
        borderRadius: '50%',
        background: `radial-gradient(circle at 50% 50%, ${hexA(P.violet, 0.22 * MOTION.drift(seg(t, 0.1, 0.7)))}, transparent 68%)`,
      }} />
    </div>
  );
}

/* 4 · VERDICT — epic. Struck from above: five lines slam down, the plate snaps
   in on an overshoot, and a flat ring runs out along the table. */
function Verdict({ r, t }) {
  const fall = MOTION.enter(seg(t, 0, 0.28));
  const snap = MOTION.pop(seg(t, 0.24, 0.56));
  const hit = seg(t, 0.26, 0.72);
  const lines = [0, -26, 26, -48, 48];
  return (
    <div style={{ position: 'absolute', left: CX, top: CY, width: 0, height: 0 }}>
      {lines.map((a, i) => {
        const g = MOTION.enter(seg(t, i * 0.03, 0.3 + i * 0.03));
        const len = r * 0.49 * g;
        return (
          <div key={i} style={{
            position: 'absolute', left: -1, top: -r * 0.62,
            width: Math.max(1.5, r * (i < 3 ? 0.0067 : 0.005)), height: len,
            transform: `rotate(${a}deg)`, transformOrigin: '50% 0%',
            background: `linear-gradient(180deg,transparent,${i < 3 ? P.pale : hexA(P.light, 0.6)})`,
            opacity: 1 - MOTION.enter(seg(t, 0.6, 1)) * 0.7,
          }} />
        );
      })}
      {hit > 0 && hit < 1 && (
        <div style={{
          position: 'absolute', left: -r * 1.0 * hit, top: -r * 0.26 * hit,
          width: r * 2.0 * hit, height: r * 0.52 * hit, borderRadius: '50%', boxSizing: 'border-box',
          border: `${Math.max(1, r * 0.01)}px solid ${hexA(P.light, (1 - hit) * 0.75)}`,
        }} />
      )}
      <div style={{
        position: 'absolute', left: -r * 0.5, top: -r * 0.5, width: r, height: r,
        transform: `rotate(45deg) scale(${mix(0.2, 1, c01(snap))}) translateY(${mix(-r * 0.3, 0, fall)}px)`,
        background: `linear-gradient(135deg,${P.pale},${P.dark})`,
        boxShadow: `0 0 ${r * 0.28}px ${hexA(P.pale, 0.5)}`,
        opacity: MOTION.enter(seg(t, 0.2, 0.34)),
      }} />
      <div style={{
        position: 'absolute', left: -r * 0.283, top: -r * 0.283, width: r * 0.566, height: r * 0.566,
        transform: `rotate(45deg) scale(${c01(snap)})`, background: '#1A1206',
      }} />
      <div style={{
        position: 'absolute', left: -r * 0.098, top: -r * 0.098, width: r * 0.196, height: r * 0.196,
        transform: `rotate(45deg) scale(${MOTION.pop(seg(t, 0.4, 0.72))})`, background: P.pale,
      }} />
    </div>
  );
}

/* 5 · ECLIPSE — relic. A bright disc, then a dark one slides across it and stops
   off-centre, leaving a burning crescent and one spark on the lit edge. */
function Eclipse({ r, t }) {
  const born = MOTION.enter(seg(t, 0, 0.24));
  const slide = MOTION.drift(seg(t, 0.2, 0.74));
  const rim = MOTION.enter(seg(t, 0.5, 0.9));
  const d = r * 1.04;
  return (
    <div style={{ position: 'absolute', left: CX, top: CY, width: 0, height: 0 }}>
      <div style={{
        position: 'absolute', left: -d / 2, top: -d / 2, width: d, height: d, borderRadius: '50%',
        background: `radial-gradient(circle at 50% 50%, ${P.pale}, ${P.gold} 62%, ${P.dark} 92%)`,
        boxShadow: `0 0 ${r * (0.2 + (1 - slide) * 0.4)}px ${hexA(P.pale, 0.4 + (1 - slide) * 0.5)}`,
        transform: `scale(${born})`,
      }} />
      <div style={{
        position: 'absolute', left: -d * 0.49 + mix(-d * 1.5, d * 0.10, slide), top: -d * 0.49,
        width: d * 0.98, height: d * 0.98, borderRadius: '50%', background: '#040302',
        boxShadow: `-${r * 0.02}px 0 ${r * 0.2}px rgba(0,0,0,.9)`,
        opacity: born,
      }} />
      <div style={{
        position: 'absolute', left: -r * 0.63, top: -r * 0.63, width: r * 1.26, height: r * 1.26,
        borderRadius: '50%', boxSizing: 'border-box', border: `${Math.max(1, r * 0.0067)}px solid ${hexA(P.pale, 0.42 * rim)}`,
      }} />
      <div style={{
        position: 'absolute', left: -r * 0.05 - r * 0.60, top: -r * 0.05,
        width: r * 0.1, height: r * 0.1, transform: 'rotate(45deg)',
        background: P.pale, opacity: rim,
        boxShadow: `0 0 ${r * 0.12}px ${hexA(P.pale, 1)}`,
      }} />
    </div>
  );
}

/* ---------------- win-screen furniture ---------------- */
function Banner({ name, note, tone, o, rise, sub }) {
  if (!cfg.banner || o <= 0.001) return null;
  return (
    <>
      <div style={{
        position: 'absolute', left: 0, right: 0, top: 62,
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 15,
        opacity: o, transform: `translateY(${rise}px)`,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
          <div style={{ width: 72, height: 1, background: `linear-gradient(90deg,transparent,${tone})` }} />
          <span style={{ font: `500 13px/1 'Oswald',sans-serif`, letterSpacing: '.42em', color: tone, whiteSpace: 'nowrap' }}>VICTORY SEAL</span>
          <div style={{ width: 72, height: 1, background: `linear-gradient(270deg,transparent,${tone})` }} />
        </div>
        <span style={{ font: `700 66px/1.2 'Cinzel',serif`, letterSpacing: '.07em', color: '#F8EED6', whiteSpace: 'nowrap', textShadow: '0 0 50px rgba(0,0,0,.9)' }}>{name}</span>
      </div>
      <div style={{
        position: 'absolute', left: 0, right: 0, bottom: 118,
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 14, opacity: o,
      }}>
        <div style={{
          display: 'flex', alignItems: 'center', gap: 11, padding: '10px 20px',
          background: 'rgba(0,0,0,.5)', border: `1px solid ${hexA(tone, 0.55)}`,
        }}>
          <i style={{ width: 8, height: 8, display: 'block', background: tone, transform: 'rotate(45deg)' }} />
          <span style={{ font: `500 13px/1 'Oswald',sans-serif`, letterSpacing: '.28em', color: tone, whiteSpace: 'nowrap' }}>{sub}</span>
        </div>
        <span style={{ font: `400 18px/1.5 'Spectral',serif`, color: P.muted, whiteSpace: 'nowrap' }}>{note}</span>
      </div>
    </>
  );
}

/* the reel: all five at their own phase, for comparing them side by side */
const REEL = [
  { C: Brand, name: 'Brand', tone: P.brandLit },
  { C: Shatter, name: 'Shatter', tone: P.teal },
  { C: Bloom, name: 'Bloom', tone: P.violet },
  { C: Verdict, name: 'Verdict', tone: P.light },
  { C: Eclipse, name: 'Eclipse', tone: P.pale },
];

function Reel({ t }) {
  const r = 118;
  return (
    <>
      {REEL.map((s, i) => {
        const x = CX + (i - 2) * 240;
        const own = c01((t * 1.25 - i * 0.05) % 1.25);
        return (
          <div key={s.name} style={{ position: 'absolute', left: 0, top: 0 }}>
            <div style={{
              position: 'absolute', left: x - 112, top: CY - 122, width: 224, height: 244,
              border: `1px solid ${hexA(s.tone, 0.24)}`, background: 'rgba(0,0,0,.24)',
            }} />
            <div style={{ position: 'absolute', left: x - CX, top: 0, width: 0, height: 0 }}>
              <s.C r={r} t={own} />
            </div>
            <div style={{
              position: 'absolute', left: x, top: CY + 142, transform: 'translateX(-50%)',
              display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 7,
            }}>
              <span style={{ font: `600 19px/1.2 'Cinzel',serif`, letterSpacing: '.06em', color: '#F1DFB8', whiteSpace: 'nowrap' }}>{s.name}</span>
              <div style={{ width: 148, height: 3, background: 'rgba(0,0,0,.5)' }}>
                <div style={{ width: `${own * 100}%`, height: '100%', background: s.tone }} />
              </div>
            </div>
          </div>
        );
      })}
    </>
  );
}

/* ================= SCENES =================
   Each scene is one seal's full stamp: it starts on an empty stage and ends on
   the still frame the catalogue shows, held for the last ~28% of the beat. */
function beat(Seal, name, note, sub, tone, toneStage) {
  return function Scene() {
    const { progress: p } = useScene();
    if (cfg.reel) {
      return (
        <>
          <Stage tone={P.gold} />
          <Reel t={p} />
          <div style={{
            position: 'absolute', left: 0, right: 0, top: 68,
            display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 13,
          }}>
            <span style={{ font: `500 13px/1 'Oswald',sans-serif`, letterSpacing: '.42em', color: P.dim }}>VICTORY SEALS</span>
            <span style={{ font: `700 46px/1.2 'Cinzel',serif`, letterSpacing: '.05em', color: '#F1DFB8' }}>All Five, Side by Side</span>
          </div>
          <div style={{ position: 'absolute', left: 0, right: 0, bottom: 74, textAlign: 'center' }}>
            <span style={{ font: `400 17px/1 'Spectral',serif`, color: P.muted }}>Each runs on its own offset so the differences in arrival are visible at once.</span>
          </div>
        </>
      );
    }
    // the seal completes by p = 0.72, then holds
    const t = c01(p / 0.72);
    const inn = MOTION.enter(seg(p, 0.1, 0.4));
    const out = 1 - MOTION.enter(seg(p, 0.94, 1));
    return (
      <>
        <Stage tone={toneStage} lift={MOTION.enter(seg(p, 0, 0.5))} flash={Math.sin(Math.PI * seg(p, 0.12, 0.44)) * 0.34} />
        <Seal r={R_FULL} t={t} />
        <Banner name={name} note={note} sub={sub} tone={tone} o={inn * out} rise={mix(14, 0, inn)} />
        <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: MOTION.enter(seg(p, 0.94, 1)) }} />
      </>
    );
  };
}

const SBrand = beat(Brand, 'Brand', 'The mark is burned in, then left to cool.', 'COMMON · 850 COINS', P.brandLit, P.brand);
const SShatter = beat(Shatter, 'Shatter', 'Your seal lands whole, then breaks in three.', 'RARE · 1 600 COINS', P.teal, P.teal);
const SBloom = beat(Bloom, 'Bloom', 'Four rings open outward. No impact anywhere in it.', 'EPIC · 200 SHARDS', P.violet, P.violet);
const SVerdict = beat(Verdict, 'Verdict', 'Struck from above and driven into the table.', 'EPIC · 340 SHARDS', P.light, P.gold);
const SEclipse = beat(Eclipse, 'Eclipse', 'A dark disc crosses it and leaves a burning rim.', 'RELIC · 360 SHARDS', P.pale, P.gold);

/* ================= root ================= */
function VictorySeals() {
  const { useTweaks, TweaksPanel, TweakSection, TweakToggle } = window;
  const [t, setTweak] = useTweaks(window.TWEAK_DEFAULTS);
  cfg.reel = t.reel;
  cfg.banner = t.banner;
  return (
    <>
      <SceneStage width={W} height={H} scenes={window.OM_SCENES} playback={window.OM_PLAYBACK} bg="#0A0705">
        {{ Brand: SBrand, Shatter: SShatter, Bloom: SBloom, Verdict: SVerdict, Eclipse: SEclipse }}
      </SceneStage>
      <TweaksPanel>
        <TweakSection label="View" />
        <TweakToggle label="Compare all five" value={t.reel} onChange={(v) => setTweak('reel', v)} />
        <TweakToggle label="Win screen text" value={t.banner} onChange={(v) => setTweak('banner', v)} />
        <TweakSection label="Editing" />
        <TweakToggle label="Motion editor" value={t.motionEditor} onChange={(v) => setTweak('motionEditor', v)} />
      </TweaksPanel>
    </>
  );
}

window.VictorySeals = VictorySeals;
