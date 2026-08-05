// rank-up.jsx — RELIQUARY promotion sequence, for any of the nine promotions.
// One generic Seal renders every rank: the metals come from the table, and the
// layer set is gated by rank so the emblem gains a layer per promotion. The
// sequence shatters rank N−1 and forges rank N.

const { SceneStage, useScene } = window;

const P = {
  gold: '#C8A45C', light: '#EBCE8A', pale: '#F8EED6', dark: '#7A5A1E',
  good: '#7ACD96', dim: '#9C8A6A',
};

/* rank 1..10 — metals, RP band, and the dark tone the stage tints toward */
const RANKS = [
  { n: 1, name: 'ASH SEAL', m1: '#6E6A62', m2: '#35322D', edge: '#8A857B', text: '#CFCAC0', bg: '#2A2823', lo: 0, hi: 400 },
  { n: 2, name: 'CLAY SEAL', m1: '#A5714A', m2: '#4A2F1C', edge: '#C08A5E', text: '#E6C4A6', bg: '#2E1D10', lo: 400, hi: 800 },
  { n: 3, name: 'COPPER SEAL', m1: '#C57B45', m2: '#5A3016', edge: '#E09A5C', text: '#F2CBA6', bg: '#341D0E', lo: 800, hi: 1200 },
  { n: 4, name: 'IRON SEAL', m1: '#8F9AA5', m2: '#3A424B', edge: '#AEB9C4', text: '#DCE3EA', bg: '#232A31', lo: 1200, hi: 1600 },
  { n: 5, name: 'SILVER SEAL', m1: '#D6DCE4', m2: '#6E7783', edge: '#F0F4F8', text: '#F4F7FA', bg: '#2A2E33', lo: 1600, hi: 2100 },
  { n: 6, name: 'GOLD SEAL', m1: '#F6E4B4', m2: '#8E6A22', edge: '#EBCE8A', text: '#F5EBD4', bg: '#3A2818', lo: 2100, hi: 2600 },
  { n: 7, name: 'OBSIDIAN SEAL', m1: '#5A5470', m2: '#16131F', edge: '#8A82A8', text: '#D6D0EA', bg: '#241F36', lo: 2600, hi: 3200 },
  { n: 8, name: 'AMBER SEAL', m1: '#F0A54A', m2: '#7A3D0C', edge: '#FFC978', text: '#FFDCA8', bg: '#3A2410', lo: 3200, hi: 3800 },
  { n: 9, name: 'RELIC SEAL', m1: '#F8EED6', m2: '#A6802F', edge: '#F3DDA4', text: '#F8EED6', bg: '#3E2C16', lo: 3800, hi: 4500 },
  { n: 10, name: 'VAULT SEAL', m1: '#EFE7FA', m2: '#5E4E8C', edge: '#EFE7FA', text: '#F6F1FE', bg: '#2A2148', lo: 4500, hi: null },
];

const OPPONENTS = ['Bexley', 'lanternjaw', 'ORRIN_VT', 'Ilse.', 'MECHATIDE', 'Kestrel_09', 'quartzfang', 'nachtfalter', 'halcyon', 'Vhalor'];

const c01 = (v) => (v < 0 ? 0 : v > 1 ? 1 : v);
const seg = (p, a, b) => c01((p - a) / (b - a));
const mix = (a, b, t) => a + (b - a) * t;
const num = (v) => Math.round(v).toLocaleString('en-US').replace(/,/g, ' ');
const hexA = (hex, a) => {
  const h = hex.replace('#', '');
  const r = parseInt(h.slice(0, 2), 16), g = parseInt(h.slice(2, 4), 16), b = parseInt(h.slice(4, 6), 16);
  return `rgba(${r},${g},${b},${a})`;
};

const MOTION = {
  enter: (p) => 1 - Math.pow(1 - c01(p), 3),
  drift: (p) => 0.5 - 0.5 * Math.cos(Math.PI * c01(p)),
  pop: (p) => { const t = c01(p), s = 1.9; return 1 + (s + 1) * Math.pow(t - 1, 3) + s * Math.pow(t - 1, 2); },
};

const W = 1280, H = 720;
const CX = W / 2, CY = 336;

/* which layers a rank owns — see README-progression.md */
const has = (rank, layer) => ({
  outer: 1, core: 2, inner: 3, axis: 4, sidePips: 5, cornerPips: 6, ring: 7, spokes: 8, filled: 9, halo: 10,
}[layer] <= rank);

const cfg = { into: 7, gain: 25, rewards: true };

/* ---------------- stage ---------------- */
function Table({ scale = 1, bg = '#2A1C12', warm = '#2A1C12', toward = 0 }) {
  return (
    <div style={{ position: 'absolute', inset: 0, overflow: 'hidden' }}>
      <div style={{
        position: 'absolute', inset: -60, transform: `scale(${scale})`, transformOrigin: '50% 41%',
        background: `radial-gradient(ellipse 900px 560px at 50% 41%, ${toward > 0.5 ? bg : warm}, #0A0705 78%)`,
      }} />
      <div style={{
        position: 'absolute', inset: 0,
        background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.045) 0 1px,transparent 1px 26px),repeating-linear-gradient(-45deg,rgba(200,164,92,.045) 0 1px,transparent 1px 26px)',
      }} />
      <div style={{
        position: 'absolute', left: CX, top: CY, width: 620, height: 620,
        transform: 'translate(-50%,-50%) rotate(45deg)', border: '1px solid rgba(200,164,92,.08)',
      }} />
      <div style={{ position: 'absolute', inset: 0, boxShadow: 'inset 0 0 200px rgba(0,0,0,.88)' }} />
    </div>
  );
}

/* ---------------- the seal ----------------
   `scatter` shatters it outward (used on the old rank), `forge` assembles it
   from the core outward (used on the new rank). Pass one or the other.
   The outer diamond is four quadrant clips of one square, so the quadrants can
   fly apart along their diagonals without the shape being redrawn. */
function Seal({ r, rank, scatter = 0, forge = 1, glow = 0.4, fade = 1 }) {
  if (fade <= 0.001 || forge <= 0.001) return null;
  const R = RANKS[rank - 1];
  const out = r * 0.846, inner = r * 0.5;
  const core = r * (has(rank, 'filled') ? 0.23 : 0.192);
  const pip = r * 0.077, ins = r * 0.0865;
  const bw = Math.max(2, r * 0.022);

  const s = MOTION.enter(scatter);
  const fly = s * r * 1.15;
  const dieOut = (at) => 1 - c01(scatter / at);

  // forge windows — each layer has its own slice of `forge`
  const wCore = MOTION.pop(seg(forge, 0, 0.26));
  const wOut = MOTION.pop(seg(forge, 0.16, 0.5));
  const wInner = MOTION.pop(seg(forge, 0.32, 0.62));
  const wAxis = seg(forge, 0.44, 0.7);
  const wPips = MOTION.pop(seg(forge, 0.56, 0.82));
  const wRing = seg(forge, 0.66, 0.94);
  const wSpoke = seg(forge, 0.74, 1);

  const quad = (i) => {
    const dx = i % 2 === 0 ? -1 : 1, dy = i < 2 ? -1 : 1;
    return (
      <div key={i} style={{
        position: 'absolute', left: '50%', top: '50%', width: out * 1.5, height: out * 1.5,
        transform: `translate(calc(-50% + ${dx * fly * 0.7}px), calc(-50% + ${dy * fly * 0.7}px)) rotate(${s * dx * 16}deg)`,
        clipPath: `inset(${dy < 0 ? 0 : 50}% ${dx > 0 ? 0 : 50}% ${dy > 0 ? 0 : 50}% ${dx < 0 ? 0 : 50}%)`,
        opacity: dieOut(0.8),
      }}>
        <div style={{
          position: 'absolute', left: '50%', top: '50%', width: out, height: out,
          transform: `translate(-50%,-50%) rotate(45deg) scale(${wOut})`,
          border: `${bw}px solid ${R.edge}`,
          background: `linear-gradient(135deg,${hexA(R.m1, 0.22)},transparent 70%)`,
        }} />
      </div>
    );
  };

  // Pips are anchored from the centre in px, never inset from the r-box edge:
  // the rotated outer diamond is wider than that box, so edge insets land
  // asymmetrically. Centring happens via margins so the 45deg rotation — which
  // would otherwise send a translate(-50%) off diagonally — cannot disturb it.
  const pips = (kind) => {
    const shown = kind === 'corner' ? has(rank, 'cornerPips') : has(rank, 'sidePips');
    if (!shown) return null;
    const pipS = pip * wPips;
    const set = kind === 'corner'
      ? [[-1, -1], [1, -1], [-1, 1], [1, 1]]   // on the axis square's corners
      : [[-1, 0], [1, 0]];                     // on the outer diamond's side vertices
    const reach = kind === 'corner' ? out / 2 : out * 0.707;
    return set.map(([dx, dy], i) => (
      <div key={kind + i} style={{
        position: 'absolute', left: '50%', top: '50%', width: pipS, height: pipS,
        marginLeft: dx * reach - pipS / 2,
        marginTop: dy * reach - pipS / 2,
        background: R.edge,
        transform: `translate(${dx * fly * 0.9}px,${dy * fly * 0.9}px) rotate(45deg)`,
        opacity: c01(wPips) * dieOut(0.7),
      }} />
    ));
  };

  return (
    <div style={{
      position: 'absolute', left: CX, top: CY, width: r, height: r,
      transform: 'translate(-50%,-50%)', opacity: fade,
      filter: `drop-shadow(0 0 ${16 + glow * 58}px ${hexA(R.edge, 0.28 + glow * 0.5)})`,
    }}>
      {has(rank, 'halo') && (
        <div style={{
          position: 'absolute', left: '50%', top: '50%', width: r, height: r, borderRadius: '50%',
          transform: `translate(-50%,-50%) scale(${mix(1.4, 1, MOTION.enter(wRing))}) rotate(${scatter * 40}deg)`,
          border: `1px dashed ${hexA(R.edge, wRing * 0.5)}`, opacity: wRing * dieOut(0.85),
        }} />
      )}
      {has(rank, 'ring') && (
        <div style={{
          position: 'absolute', left: '50%', top: '50%', width: r * 0.96, height: r * 0.96, borderRadius: '50%',
          transform: `translate(-50%,-50%) scale(${mix(1.45, 1, MOTION.enter(wRing)) * (1 + s * 0.6)})`,
          border: `1px solid ${hexA(R.edge, wRing * 0.5)}`, opacity: wRing * dieOut(0.85),
        }} />
      )}
      {has(rank, 'spokes') && [0, 90].concat(has(rank, 'filled') ? [45, 135] : []).map((a) => (
        <div key={a} style={{
          position: 'absolute', left: '50%', top: '50%', width: r * (1 + s * 0.8), height: 2,
          transform: `translate(-50%,-50%) rotate(${a}deg)`,
          background: hexA(R.edge, wSpoke * 0.24), opacity: wSpoke * dieOut(0.75),
        }} />
      ))}
      {has(rank, 'axis') && (
        <div style={{
          position: 'absolute', left: '50%', top: '50%', width: out, height: out,
          transform: `translate(-50%,-50%) scale(${1 + s * 0.5})`,
          border: `1px solid ${hexA(R.edge, wAxis * 0.32)}`, opacity: wAxis * dieOut(0.8),
        }} />
      )}
      {[0, 1, 2, 3].map(quad)}
      {has(rank, 'inner') && (
        <div style={{
          position: 'absolute', left: '50%', top: '50%', width: inner, height: inner,
          transform: `translate(-50%,-50%) rotate(45deg) scale(${wInner * (1 + s * 0.9)})`,
          border: `${Math.max(1, r * 0.014)}px solid ${hexA(R.edge, 0.65)}`,
          background: has(rank, 'filled') ? `linear-gradient(135deg,${hexA(R.m1, 0.32)},${hexA(R.m2, 0.14)})` : 'none',
          opacity: c01(wInner) * dieOut(0.6),
        }} />
      )}
      {pips('side')}
      {pips('corner')}
      {has(rank, 'core') && (
        <div style={{
          position: 'absolute', left: '50%', top: '50%',
          width: core * wCore * (1 + s * 2.6), height: core * wCore * (1 + s * 2.6),
          transform: 'translate(-50%,-50%) rotate(45deg)',
          background: `linear-gradient(135deg,${R.m1},${R.m2})`,
          boxShadow: `0 0 ${r * 0.18 + glow * 38}px ${hexA(R.m1, 0.5 + glow * 0.4)}`,
          opacity: c01(wCore) * dieOut(0.5),
        }} />
      )}
    </div>
  );
}

/* ---------------- RP bar ---------------- */
function RpBar({ label, fill, rp, cap, edge, textCol, o = 1, overflow = 0, note }) {
  if (o <= 0.001) return null;
  return (
    <div style={{
      position: 'absolute', left: CX, top: 508, width: 560,
      transform: 'translateX(-50%)', opacity: o, display: 'flex', flexDirection: 'column', gap: 11,
    }}>
      <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between' }}>
        <span style={{ font: `500 13px/1 'Oswald',sans-serif`, letterSpacing: '.26em', color: textCol }}>{label}</span>
        <span style={{ font: `400 15px/1 'Spectral',serif`, color: P.dim }}>{note || `${num(rp)} / ${num(cap)} RP`}</span>
      </div>
      <div style={{
        position: 'relative', height: 16, background: 'rgba(0,0,0,.5)',
        border: `1px solid ${hexA(edge, 0.42)}`, overflow: 'hidden',
      }}>
        <div style={{
          position: 'absolute', left: 0, top: 0, bottom: 0, width: `${c01(fill) * 100}%`,
          background: `linear-gradient(90deg,${hexA(edge, 0.35)},${edge})`,
          boxShadow: overflow > 0 ? `0 0 ${18 * overflow}px ${hexA(P.pale, 0.8 * overflow)}` : 'none',
        }} />
        {overflow > 0 && (
          <div style={{
            position: 'absolute', inset: 0,
            background: `linear-gradient(90deg,transparent,rgba(255,255,255,${0.5 * overflow}) 50%,transparent)`,
          }} />
        )}
      </div>
    </div>
  );
}

/* ---------------- text furniture ---------------- */
function Eyebrow({ text, o, rise, tone }) {
  if (o <= 0.001) return null;
  return (
    <div style={{
      position: 'absolute', left: 0, right: 0, top: 74,
      display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 18,
      opacity: o, transform: `translateY(${rise}px)`,
    }}>
      <div style={{ width: 80, height: 1, background: `linear-gradient(90deg,transparent,${tone})` }} />
      <span style={{ font: `500 14px/1 'Oswald',sans-serif`, letterSpacing: '.4em', color: tone, whiteSpace: 'nowrap' }}>{text}</span>
      <div style={{ width: 80, height: 1, background: `linear-gradient(270deg,transparent,${tone})` }} />
    </div>
  );
}

function Reward({ text, tone }) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 10, padding: '11px 18px',
      background: 'rgba(0,0,0,.45)', border: `1px solid ${hexA(tone, 0.45)}`,
    }}>
      <i style={{ width: 8, height: 8, display: 'block', background: tone, transform: 'rotate(45deg)' }} />
      <span style={{ font: `400 15px/1 'Spectral',serif`, color: '#C8B189', whiteSpace: 'nowrap' }}>{text}</span>
    </div>
  );
}

/* ---------------- the promotion, resolved from cfg.into ---------------- */
function promo() {
  const into = Math.min(10, Math.max(2, cfg.into));
  const to = RANKS[into - 1];
  const from = RANKS[into - 2];
  const fromWidth = (from.hi - from.lo) / 5;
  const rp0 = to.lo - 20;                        // just under the threshold
  const rp1 = rp0 + cfg.gain;
  const toWidth = to.hi ? (to.hi - to.lo) / 5 : null;
  return {
    from, to, into,
    fromLabel: from.name + ' V',
    toLabel: to.name + ' I',
    fromFill: 1 - 20 / fromWidth,
    fromCap: to.lo,
    rp0, rp1,
    toFill: toWidth ? (rp1 - to.lo) / toWidth : 1,
    toCap: toWidth ? to.lo + toWidth : null,
    toNote: toWidth ? null : 'top 8 000 · ranked by placement',
    opponent: OPPONENTS[into - 1],
    coins: 100 + into * 25,
    unlock: into === 6 ? 'Gilded Reliquary frame'
      : into === 8 ? 'Amber Halo frame'
      : into === 10 ? 'Vault Ring frame'
      : null,
  };
}

/* ================= SCENES =================
   Every scene's first frame equals the previous scene's last frame:
   Result → Award  fill .95, rp0, old seal intact
   Award  → Break  fill 1.0, rp1, old seal intact, glowing
   Break  → Forge  old seal gone, wash .35, no bar
   Forge  → Reveal new seal complete, wash 0 */

function SceneResult() {
  const { progress: p } = useScene();
  const k = promo();
  const inn = MOTION.enter(seg(p, 0.04, 0.4));
  const g = 0.1 + Math.sin(Math.PI * seg(p, 0.3, 1)) * 0.12;
  return (
    <>
      <Table scale={mix(1.02, 1.06, MOTION.drift(p))} warm={k.from.bg} />
      <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: 1 - MOTION.enter(seg(p, 0, 0.22)) }} />
      <Seal r={182} rank={k.into - 1} glow={g} fade={inn} />
      <Eyebrow text="DUEL WON" o={inn} rise={mix(14, 0, inn)} tone={P.good} />
      <div style={{
        position: 'absolute', left: 0, right: 0, top: 118, textAlign: 'center',
        opacity: inn, transform: `translateY(${mix(16, 0, inn)}px)`,
      }}>
        <span style={{ font: `700 46px/1.2 'Cinzel',serif`, letterSpacing: '.06em', color: '#F1DFB8' }}>{k.opponent} defeated</span>
      </div>
      <RpBar label={k.fromLabel} fill={k.fromFill} rp={k.rp0} cap={k.fromCap} edge={k.from.edge} textCol={k.from.text} o={inn} />
    </>
  );
}

function SceneAward() {
  const { progress: p } = useScene();
  const k = promo();
  const count = MOTION.enter(seg(p, 0.14, 0.72));
  const cap = 1 - MOTION.enter(seg(p, 0.72, 0.94));
  const chip = MOTION.enter(seg(p, 0.04, 0.24)) * (1 - MOTION.enter(seg(p, 0.78, 1)));
  return (
    <>
      <Table scale={mix(1.06, 1.1, MOTION.drift(p))} warm={k.from.bg} />
      <Seal r={182} rank={k.into - 1} glow={0.22 + MOTION.enter(seg(p, 0.6, 1)) * 0.6} />
      <Eyebrow text="DUEL WON" o={1 - MOTION.enter(seg(p, 0.5, 0.86))} rise={mix(0, -12, MOTION.enter(seg(p, 0.5, 0.86)))} tone={P.good} />
      <div style={{
        position: 'absolute', left: 0, right: 0, top: 118, textAlign: 'center',
        opacity: 1 - MOTION.enter(seg(p, 0.42, 0.78)),
      }}>
        <span style={{ font: `700 46px/1.2 'Cinzel',serif`, letterSpacing: '.06em', color: '#F1DFB8' }}>{k.opponent} defeated</span>
      </div>
      <div style={{
        position: 'absolute', left: CX, top: 456,
        transform: `translate(-50%,${mix(20, 0, MOTION.enter(seg(p, 0.04, 0.24)))}px)`,
        opacity: chip, display: 'flex', alignItems: 'center', gap: 12,
        padding: '9px 20px', background: 'rgba(0,0,0,.5)', border: `1px solid rgba(122,205,150,.55)`,
      }}>
        <i style={{ width: 9, height: 9, display: 'block', background: P.good, transform: 'rotate(45deg)' }} />
        <span style={{ font: `600 20px/1.2 'Cinzel',serif`, color: '#A8E4BE', whiteSpace: 'nowrap' }}>+{cfg.gain} RP</span>
      </div>
      <RpBar label={k.fromLabel} fill={mix(k.fromFill, 1, count)} rp={mix(k.rp0, k.rp1, count)}
             cap={k.fromCap} edge={k.from.edge} textCol={k.from.text} overflow={1 - cap} />
    </>
  );
}

function SceneBreak() {
  const { progress: p } = useScene();
  const k = promo();
  const scatter = seg(p, 0.16, 1);
  const flash = Math.sin(Math.PI * seg(p, 0.06, 0.62));
  const barOut = 1 - MOTION.enter(seg(p, 0.3, 0.66));
  const shock = seg(p, 0.1, 0.7);
  return (
    <>
      <Table scale={mix(1.1, 1.2, MOTION.drift(p))} warm={k.from.bg} bg={k.to.bg} toward={seg(p, 0.45, 1)} />
      <Seal r={182} rank={k.into - 1} scatter={scatter} glow={mix(0.82, 0.2, MOTION.enter(scatter))} />
      {shock > 0 && shock < 1 && (
        <div style={{
          position: 'absolute', left: CX, top: CY,
          width: mix(180, 900, MOTION.enter(shock)), height: mix(180, 900, MOTION.enter(shock)),
          transform: 'translate(-50%,-50%) rotate(45deg)',
          border: `2px solid ${hexA(P.pale, (1 - shock) * 0.7)}`,
        }} />
      )}
      <div style={{
        position: 'absolute', left: CX, top: CY,
        width: 200 + flash * 1900, height: 200 + flash * 1900,
        transform: 'translate(-50%,-50%)', borderRadius: '50%',
        background: `radial-gradient(circle, ${hexA(P.pale, flash * 0.5)}, ${hexA(k.from.edge, flash * 0.16)} 36%, transparent 62%)`,
      }} />
      <RpBar label={k.fromLabel} fill={1} rp={k.rp1} cap={k.fromCap} edge={k.from.edge} textCol={k.from.text} o={barOut} overflow={barOut} />
      <div style={{ position: 'absolute', inset: 0, background: P.pale, opacity: MOTION.enter(seg(p, 0.82, 1)) * 0.35 }} />
    </>
  );
}

function SceneForge() {
  const { progress: p } = useScene();
  const k = promo();
  const forge = seg(p, 0.06, 0.9);
  return (
    <>
      <Table scale={mix(1.2, 1.08, MOTION.drift(p))} warm={k.to.bg} bg={k.to.bg} toward={1} />
      <Seal r={182} rank={k.into} forge={forge} glow={0.4 + Math.sin(Math.PI * seg(p, 0.2, 1)) * 0.4} />
      <Eyebrow text="A NEW SEAL" o={MOTION.enter(seg(p, 0.5, 0.86))} rise={mix(14, 0, MOTION.enter(seg(p, 0.5, 0.86)))} tone={k.to.edge} />
      <div style={{ position: 'absolute', inset: 0, background: P.pale, opacity: (1 - MOTION.enter(seg(p, 0, 0.34))) * 0.35 }} />
    </>
  );
}

function SceneReveal() {
  const { progress: p } = useScene();
  const k = promo();
  const inn = MOTION.enter(seg(p, 0.04, 0.34));
  const bar = MOTION.enter(seg(p, 0.3, 0.56));
  const rew = MOTION.enter(seg(p, 0.44, 0.68));
  const out = 1 - MOTION.enter(seg(p, 0.94, 1));
  const breathe = Math.sin(Math.PI * 2 * seg(p, 0.2, 1) - Math.PI / 2) * 0.5 + 0.5;
  return (
    <>
      <Table scale={mix(1.08, 1.04, MOTION.drift(p))} warm={k.to.bg} bg={k.to.bg} toward={1} />
      <Seal r={182} rank={k.into} forge={1} glow={(0.42 + breathe * 0.3) * out} />
      <Eyebrow text="A NEW SEAL" o={out} rise={0} tone={k.to.edge} />
      <div style={{
        position: 'absolute', left: 0, right: 0, top: 122,
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 16,
        opacity: inn * out, transform: `translateY(${mix(22, 0, inn)}px)`,
      }}>
        <span style={{
          font: `700 62px/1.2 'Cinzel',serif`, letterSpacing: '.07em', color: k.to.text,
          whiteSpace: 'nowrap', textShadow: `0 0 50px ${hexA(k.to.edge, 0.5)}`,
        }}>{k.toLabel}</span>
      </div>
      <div style={{
        position: 'absolute', left: CX, top: 462, transform: 'translateX(-50%)',
        display: 'flex', alignItems: 'center', gap: 9, opacity: inn * out,
      }}>
        {[0, 1, 2, 3, 4].map((i) => (
          <i key={i} style={{
            width: 13, height: 13, display: 'block', transform: 'rotate(45deg)',
            background: i === 0 ? k.to.edge : 'transparent',
            border: i === 0 ? 'none' : `1px solid ${hexA(k.to.edge, 0.5)}`,
          }} />
        ))}
      </div>
      <RpBar label={k.toLabel} fill={k.toFill} rp={k.rp1} cap={k.toCap} note={k.toNote}
             edge={k.to.edge} textCol={k.to.text} o={bar * out} />
      {cfg.rewards && (
        <div style={{
          position: 'absolute', left: CX, top: 588, transform: 'translateX(-50%)',
          display: 'flex', gap: 14, opacity: rew * out,
        }}>
          <Reward text="1 Sealed Pack" tone={P.light} />
          <Reward text={`${num(k.coins)} Coins`} tone={P.light} />
          {k.unlock && <Reward text={k.unlock} tone={k.to.edge} />}
        </div>
      )}
      <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: MOTION.enter(seg(p, 0.94, 1)) }} />
    </>
  );
}

/* ================= root ================= */
function RankUp() {
  const { useTweaks, TweaksPanel, TweakSection, TweakSlider, TweakToggle } = window;
  const [t, setTweak] = useTweaks(window.TWEAK_DEFAULTS);
  cfg.into = t.into;
  cfg.gain = t.gain;
  cfg.rewards = t.rewards;
  const name = RANKS[Math.min(10, Math.max(2, t.into)) - 1].name;
  return (
    <>
      <SceneStage width={W} height={H} scenes={window.OM_SCENES} playback={window.OM_PLAYBACK} bg="#0A0705">
        {{ Result: SceneResult, Award: SceneAward, Break: SceneBreak, Forge: SceneForge, Reveal: SceneReveal }}
      </SceneStage>
      <TweaksPanel>
        <TweakSection label={'Promotion → ' + name + ' I'} />
        <TweakSlider label="Into rank" value={t.into} min={2} max={10} step={1}
                     onChange={(v) => setTweak('into', v)} />
        <TweakSection label="Award" />
        <TweakSlider label="RP gained" value={t.gain} min={5} max={40} step={1} unit=" RP"
                     onChange={(v) => setTweak('gain', v)} />
        <TweakToggle label="Show rewards" value={t.rewards} onChange={(v) => setTweak('rewards', v)} />
        <TweakSection label="Editing" />
        <TweakToggle label="Motion editor" value={t.motionEditor} onChange={(v) => setTweak('motionEditor', v)} />
      </TweaksPanel>
    </>
  );
}

window.RankUp = RankUp;
