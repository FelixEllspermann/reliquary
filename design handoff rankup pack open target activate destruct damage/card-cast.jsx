// card-cast.jsx — RELIQUARY in-duel card activation and targeting.
// Two beats the player needs to read instantly: "my card is going off" and
// "that one is being hit". The first is a lift and a slam, the second is a
// reticle that travels, locks, and releases.

const { SceneStage, useScene } = window;

const P = {
  gold: '#C8A45C', light: '#EBCE8A', pale: '#F8EED6', dark: '#7A5A1E',
  parch1: '#EBE1C7', parch2: '#D9CCAB',
  ember: '#E0603A', emberLit: '#F3C3A6', teal: '#8FC6D2', violet: '#B9A3E0',
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
const CW = 132, CH = 185;          // card at field scale
const HAND_Y = 660;                // hand cards sit mostly off the bottom edge
const SLOT = { x: 470, y: 452 };   // the player's field slot the card lands in
const FOE = { x: 700, y: 178 };    // the opponent monster that gets targeted
const HAND_X = [372, 470, 568, 666, 764];

const cfg = { reticle: 1, damage: true };

/* ---------------- the field ---------------- */
function Field({ dim = 0, shake = 0 }) {
  const sx = Math.sin(shake * Math.PI * 11) * shake * 6;
  const sy = Math.cos(shake * Math.PI * 8) * shake * 4;
  return (
    <div style={{ position: 'absolute', inset: 0, overflow: 'hidden', transform: `translate(${sx}px,${sy}px)` }}>
      <div style={{
        position: 'absolute', inset: -40,
        background: 'radial-gradient(ellipse 980px 620px at 50% 48%, #2A1C12, #0A0705 78%)',
      }} />
      <div style={{
        position: 'absolute', inset: 0,
        background: 'repeating-linear-gradient(45deg,rgba(200,164,92,.04) 0 1px,transparent 1px 26px),repeating-linear-gradient(-45deg,rgba(200,164,92,.04) 0 1px,transparent 1px 26px)',
      }} />
      <div style={{ position: 'absolute', left: 0, right: 0, top: 316, height: 1, background: hexA(P.gold, 0.22) }} />
      <div style={{
        position: 'absolute', left: W / 2, top: 316, width: 22, height: 22,
        transform: 'translate(-50%,-50%) rotate(45deg)', background: '#0A0705', border: `1px solid ${hexA(P.gold, 0.4)}`,
      }} />
      {/* five zones per side */}
      {[178, 452].map((y) => HAND_X.map((x, i) => (
        <div key={y + '-' + i} style={{
          position: 'absolute', left: x, top: y, width: CW + 8, height: CH + 8,
          transform: 'translate(-50%,-50%)', boxSizing: 'border-box',
          border: `1px solid ${hexA(P.gold, 0.13)}`,
        }} />
      )))}
      <div style={{ position: 'absolute', inset: 0, boxShadow: 'inset 0 0 190px rgba(0,0,0,.9)' }} />
      {dim > 0 && <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: dim * 0.55 }} />}
    </div>
  );
}

/* ---------------- cards ---------------- */
function CardBack({ w = CW, h = CH }) {
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
      <div style={{
        position: 'absolute', left: '50%', top: '50%', width: 62, height: 62,
        transform: 'translate(-50%,-50%) rotate(45deg)', border: `2px solid ${hexA(P.gold, 0.6)}`,
      }} />
      <div style={{
        position: 'absolute', left: '50%', top: '50%', width: 30, height: 30,
        transform: 'translate(-50%,-50%) rotate(45deg)',
        background: `linear-gradient(135deg,${hexA(P.gold, 0.35)},${hexA(P.gold, 0.05)})`,
        border: `1px solid ${hexA(P.gold, 0.7)}`,
      }} />
      <div style={{
        position: 'absolute', left: '50%', top: '50%', width: 13, height: 13,
        transform: 'translate(-50%,-50%) rotate(45deg)', background: `linear-gradient(135deg,${P.light},${P.dark})`,
      }} />
    </div>
  );
}

// A field-scale card front. `charge` lights the frame and the effect box, which is
// how activation reads at a glance; `hurt` tints the whole card toward the hit.
function CardFront({ d, w = CW, h = CH, charge = 0, hurt = 0 }) {
  const pad = w * 0.104;
  const inner = w - pad * 2;
  const art = inner * 0.92;
  return (
    <div style={{
      position: 'relative', width: w, height: h, borderRadius: 5, overflow: 'hidden',
      boxSizing: 'border-box', padding: `${h * 0.03}px ${pad}px`,
      background: 'linear-gradient(165deg,#332315,#150d07 55%,#251809)',
      border: `2px solid ${charge > 0.02 ? P.pale : d.edge}`,
      boxShadow: charge > 0.02 ? `inset 0 0 ${20 * charge}px ${hexA(P.light, 0.5 * charge)}` : 'none',
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
            <div style={{
              position: 'absolute', inset: 1.5, background: 'linear-gradient(160deg,#3B2A10,#180F04)',
              clipPath: 'polygon(50% 0,100% 20%,100% 66%,50% 100%,0 66%,0 20%)',
            }} />
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
            flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '0 4px',
            background: 'rgba(0,0,0,.35)', border: `1px solid ${hexA(P.gold, 0.45)}`,
          }}>
            <span style={{ display: 'flex', alignItems: 'center', gap: 3, font: `500 ${h * 0.028}px/1 'Oswald',sans-serif`, letterSpacing: '.08em', color: '#E4D3AE' }}>
              <i style={{ width: 4, height: 4, display: 'block', background: d.attrC, transform: 'rotate(45deg)' }} />{d.attr}
            </span>
          </div>
        </div>
        <div style={{
          height: d.dmg ? h * 0.16 : h * 0.235, boxSizing: 'border-box', padding: '4px 5px',
          background: charge > 0.02
            ? `linear-gradient(180deg,${P.pale},${P.parch1})`
            : `linear-gradient(180deg,${P.parch1},${P.parch2})`,
          border: `1px solid ${charge > 0.02 ? P.light : '#8C7440'}`,
          display: 'flex', flexDirection: 'column', gap: 2.5,
        }}>
          <div style={{ width: '94%', height: 2.5, background: 'rgba(46,36,23,.32)' }} />
          <div style={{ width: '86%', height: 2.5, background: 'rgba(46,36,23,.32)' }} />
          <div style={{ width: '91%', height: 2.5, background: 'rgba(46,36,23,.32)' }} />
          {!d.dmg && <div style={{ width: '72%', height: 2.5, background: 'rgba(46,36,23,.32)' }} />}
          <div style={{ width: '56%', height: 2.5, background: 'rgba(46,36,23,.32)' }} />
        </div>
        {d.dmg && (
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
        )}
      </div>
      {hurt > 0.01 && (
        <div style={{ position: 'absolute', inset: 0, background: hexA(P.ember, hurt * 0.5), mixBlendMode: 'screen' }} />
      )}
    </div>
  );
}

const SPELL = { edge: P.teal, kind: 'SPELL', attr: 'WATER', attrC: P.teal, lv: 2, dmg: null, def: null };
const TARGET = { edge: P.violet, kind: 'MONSTER', attr: 'DARK', attrC: P.violet, lv: 3, dmg: '2600', def: '2100' };
const ALLY = { edge: P.gold, kind: 'MONSTER', attr: 'FIRE', attrC: P.ember, lv: 2, dmg: '1800', def: '1500' };

/* ---------------- the opponent's board ---------------- */
function Foe({ hurt = 0, lock = 0, shrink = 0 }) {
  return (
    <div style={{
      position: 'absolute', left: FOE.x, top: FOE.y,
      transform: `translate(-50%,-50%) scale(${1 - shrink * 0.06})`,
      filter: lock > 0.02 ? `drop-shadow(0 0 ${16 + lock * 30}px ${hexA(P.ember, 0.3 + lock * 0.5)})` : 'none',
    }}>
      <CardFront d={TARGET} hurt={hurt} />
    </div>
  );
}

/* ---------------- targeting reticle ----------------
   Four brackets that close in on the target, plus a diamond that spins down to
   rest. `travel` moves it from the caster to the target; `lock` closes it. */
function Reticle({ x, y, lock = 0, o = 1, spin = 0 }) {
  if (o <= 0.001 || cfg.reticle <= 0) return null;
  const reach = mix(120, 8, MOTION.enter(lock));
  const arm = 26;
  const bracket = (dx, dy) => (
    <div key={`${dx}${dy}`} style={{
      position: 'absolute', left: '50%', top: '50%', width: arm, height: arm,
      marginLeft: dx * (CW / 2 + reach) - (dx > 0 ? arm : 0),
      marginTop: dy * (CH / 2 + reach) - (dy > 0 ? arm : 0),
      borderLeft: dx < 0 ? `3px solid ${P.ember}` : 'none',
      borderRight: dx > 0 ? `3px solid ${P.ember}` : 'none',
      borderTop: dy < 0 ? `3px solid ${P.ember}` : 'none',
      borderBottom: dy > 0 ? `3px solid ${P.ember}` : 'none',
    }} />
  );
  return (
    <div style={{
      position: 'absolute', left: x, top: y, width: 0, height: 0,
      opacity: o * cfg.reticle,
      filter: `drop-shadow(0 0 ${8 + lock * 16}px ${hexA(P.ember, 0.6)})`,
    }}>
      {[[-1, -1], [1, -1], [-1, 1], [1, 1]].map(([dx, dy]) => bracket(dx, dy))}
      <div style={{
        position: 'absolute', left: '50%', top: '50%', width: 15, height: 15,
        marginLeft: -7.5, marginTop: -7.5,
        transform: `rotate(${45 + spin}deg) scale(${mix(1.6, 1, MOTION.enter(lock))})`,
        border: `2px solid ${P.emberLit}`,
      }} />
    </div>
  );
}

/* a thread of light from caster to target */
function Thread({ from, to, t, o }) {
  if (o <= 0.001) return null;
  const dx = to.x - from.x, dy = to.y - from.y;
  const len = Math.sqrt(dx * dx + dy * dy);
  const ang = Math.atan2(dy, dx) * 180 / Math.PI;
  return (
    <div style={{
      position: 'absolute', left: from.x, top: from.y, width: len * c01(t), height: 2,
      transform: `rotate(${ang}deg)`, transformOrigin: '0 50%', opacity: o,
      background: `linear-gradient(90deg,${hexA(P.ember, 0.1)},${hexA(P.emberLit, 0.85)})`,
      boxShadow: `0 0 12px ${hexA(P.ember, 0.7)}`,
    }} />
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

/* the hand: four backs plus the one card being played, drawn separately */
function Hand({ dim = 0, pulled = 1 }) {
  return [0, 1, 3, 4].map((i, n) => (
    <div key={i} style={{
      position: 'absolute', left: 300 + i * 118, top: HAND_Y + 26,
      transform: `translate(-50%,-50%) rotate(${(i - 2) * 5}deg) scale(.86)`,
      opacity: 1 - dim * 0.55,
      filter: 'drop-shadow(0 -8px 22px rgba(0,0,0,.7))',
    }}>
      <CardBack />
    </div>
  ));
}

/* ================= SCENES =================
   ┌──────────┬──────────────┬────────┬────────┬─────────┐
   │ scene    │ card pos     │ charge │ thread │ reticle │
   ├──────────┼──────────────┼────────┼────────┼─────────┤
   │ Idle     │ hand         │ 0      │ 0      │ 0       │
   │ Lift     │ hand → above │ 0 → .4 │ 0      │ 0       │
   │ Activate │ → slot       │ .4 → 1 │ 0      │ 0       │
   │ Target   │ slot         │ 1 → .6 │ 0 → 1  │ 0 → 1   │
   │ Resolve  │ slot         │ .6 → 0 │ 1 → 0  │ 1 → 0   │
   └──────────┴──────────────┴────────┴────────┴─────────┘ */

const LIFT_Y = 356;   // where the card hangs while being read

function SceneIdle() {
  const { progress: p } = useScene();
  const inn = MOTION.enter(seg(p, 0.04, 0.34));
  const hint = 0.5 + Math.sin(Math.PI * 2 * seg(p, 0.3, 1)) * 0.5;
  return (
    <>
      <Field />
      <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: 1 - MOTION.enter(seg(p, 0, 0.2)) }} />
      <Foe />
      <div style={{ position: 'absolute', left: HAND_X[1], top: SLOT.y, transform: 'translate(-50%,-50%)' }}>
        <CardFront d={ALLY} />
      </div>
      <Hand />
      <div style={{
        position: 'absolute', left: 300 + 2 * 118, top: HAND_Y + 26 - hint * 12,
        transform: 'translate(-50%,-50%) scale(.86)', opacity: inn,
        filter: `drop-shadow(0 -10px 24px rgba(0,0,0,.75)) drop-shadow(0 0 ${10 + hint * 16}px ${hexA(P.teal, 0.3 + hint * 0.3)})`,
      }}>
        <CardFront d={SPELL} />
      </div>
      <Label text="YOUR TURN" sub="Main phase · 1 activation left" o={inn} rise={mix(12, 0, inn)} tone={P.dim} />
    </>
  );
}

function SceneLift() {
  const { progress: p } = useScene();
  const t = MOTION.enter(seg(p, 0.06, 0.78));
  const dim = MOTION.enter(seg(p, 0.1, 0.6));
  return (
    <>
      <Field dim={dim} />
      <Foe />
      <div style={{ position: 'absolute', left: HAND_X[1], top: SLOT.y, transform: 'translate(-50%,-50%)', opacity: 1 - dim * 0.5 }}>
        <CardFront d={ALLY} />
      </div>
      <Hand dim={dim} />
      <div style={{
        position: 'absolute', left: mix(300 + 2 * 118, W / 2, t), top: mix(HAND_Y + 26, LIFT_Y, t),
        transform: `translate(-50%,-50%) rotate(${mix(0, 0, t)}deg) scale(${mix(0.86, 1.62, t)})`,
        filter: `drop-shadow(0 26px 50px rgba(0,0,0,.8)) drop-shadow(0 0 ${12 + t * 34}px ${hexA(P.teal, 0.25 + t * 0.4)})`,
      }}>
        <CardFront d={SPELL} charge={t * 0.4} />
      </div>
      <Label text="ACTIVATING" o={MOTION.enter(seg(p, 0.34, 0.72))} rise={mix(12, 0, MOTION.enter(seg(p, 0.34, 0.72)))} tone={P.teal} />
    </>
  );
}

function SceneActivate() {
  const { progress: p } = useScene();
  const slam = MOTION.enter(seg(p, 0.16, 0.54));
  const ring = seg(p, 0.44, 0.96);
  const flash = Math.sin(Math.PI * seg(p, 0.4, 0.78));
  const shake = Math.sin(Math.PI * seg(p, 0.46, 0.78)) * 0.7;
  const charge = mix(0.4, 1, MOTION.enter(seg(p, 0, 0.6)));
  return (
    <>
      <Field dim={1 - MOTION.enter(seg(p, 0.5, 1)) * 0.45} shake={shake} />
      <Foe />
      <div style={{ position: 'absolute', left: HAND_X[1], top: SLOT.y, transform: 'translate(-50%,-50%)', opacity: 0.62 }}>
        <CardFront d={ALLY} />
      </div>
      <Hand dim={1} />
      {ring > 0 && ring < 1 && (
        <>
          <div style={{
            position: 'absolute', left: SLOT.x, top: SLOT.y,
            width: mix(120, 620, MOTION.enter(ring)), height: mix(120, 620, MOTION.enter(ring)) * 0.42,
            transform: 'translate(-50%,-50%)', borderRadius: '50%',
            border: `2px solid ${hexA(P.teal, (1 - ring) * 0.8)}`,
          }} />
          <div style={{
            position: 'absolute', left: SLOT.x, top: SLOT.y,
            width: mix(90, 420, MOTION.enter(seg(ring, 0.18, 1))), height: mix(90, 420, MOTION.enter(seg(ring, 0.18, 1))),
            transform: 'translate(-50%,-50%) rotate(45deg)',
            border: `1px solid ${hexA(P.pale, (1 - ring) * 0.55)}`,
          }} />
        </>
      )}
      <div style={{
        position: 'absolute', left: SLOT.x, top: SLOT.y,
        width: 160 + flash * 520, height: 160 + flash * 520,
        transform: 'translate(-50%,-50%)', borderRadius: '50%',
        background: `radial-gradient(circle, ${hexA(P.pale, flash * 0.4)}, ${hexA(P.teal, flash * 0.14)} 38%, transparent 64%)`,
      }} />
      <div style={{
        position: 'absolute', left: mix(W / 2, SLOT.x, slam), top: mix(LIFT_Y, SLOT.y, slam),
        transform: `translate(-50%,-50%) scale(${mix(1.62, 1, slam)})`,
        filter: `drop-shadow(0 ${mix(26, 10, slam)}px ${mix(50, 22, slam)}px rgba(0,0,0,.8)) drop-shadow(0 0 ${34 * charge}px ${hexA(P.teal, 0.5 * charge)})`,
      }}>
        <CardFront d={SPELL} charge={charge} />
      </div>
      <Label text="SPELL ACTIVATED" o={MOTION.enter(seg(p, 0.52, 0.86))} rise={mix(12, 0, MOTION.enter(seg(p, 0.52, 0.86)))} tone={P.teal} />
    </>
  );
}

function SceneTarget() {
  const { progress: p } = useScene();
  const thread = MOTION.enter(seg(p, 0.08, 0.46));
  const travel = MOTION.enter(seg(p, 0.12, 0.52));
  const lock = MOTION.enter(seg(p, 0.5, 0.86));
  const charge = mix(1, 0.6, MOTION.enter(seg(p, 0.3, 1)));
  const rx = mix(SLOT.x, FOE.x, travel);
  const ry = mix(SLOT.y - 40, FOE.y, travel);
  return (
    <>
      <Field dim={0.55} />
      <Foe lock={lock} />
      <div style={{ position: 'absolute', left: HAND_X[1], top: SLOT.y, transform: 'translate(-50%,-50%)', opacity: 0.55 }}>
        <CardFront d={ALLY} />
      </div>
      <Hand dim={1} />
      <div style={{
        position: 'absolute', left: SLOT.x, top: SLOT.y, transform: 'translate(-50%,-50%)',
        filter: `drop-shadow(0 10px 22px rgba(0,0,0,.8)) drop-shadow(0 0 ${30 * charge}px ${hexA(P.teal, 0.45 * charge)})`,
      }}>
        <CardFront d={SPELL} charge={charge} />
      </div>
      <Thread from={{ x: SLOT.x, y: SLOT.y - 40 }} to={FOE} t={thread} o={mix(0.9, 0.45, lock)} />
      <Reticle x={rx} y={ry} lock={lock} spin={mix(120, 0, MOTION.enter(seg(p, 0.12, 0.86)))} />
      <Label text="SELECT TARGET" sub="Destroy 1 monster your opponent controls"
             o={MOTION.enter(seg(p, 0.04, 0.34))} rise={mix(12, 0, MOTION.enter(seg(p, 0.04, 0.34)))} tone={P.ember} />
    </>
  );
}

function SceneResolve() {
  const { progress: p } = useScene();
  const hit = seg(p, 0.06, 0.26);
  const hurt = Math.sin(Math.PI * seg(p, 0.04, 0.42));
  const shake = Math.sin(Math.PI * seg(p, 0.04, 0.34)) * 0.9;
  const rise = MOTION.enter(seg(p, 0.1, 0.62));
  const relOut = 1 - MOTION.enter(seg(p, 0.2, 0.5));
  const shrink = MOTION.enter(seg(p, 0.34, 0.86));
  const out = 1 - MOTION.enter(seg(p, 0.92, 1));
  return (
    <>
      <Field dim={mix(0.55, 0, MOTION.enter(seg(p, 0.4, 1)))} shake={shake} />
      <div style={{ opacity: 1 - shrink * 0.85 }}>
        <Foe hurt={hurt} lock={relOut} shrink={shrink} />
      </div>
      {hit > 0 && hit < 1 && (
        <div style={{
          position: 'absolute', left: FOE.x, top: FOE.y,
          width: mix(120, 520, MOTION.enter(hit)), height: mix(120, 520, MOTION.enter(hit)),
          transform: 'translate(-50%,-50%) rotate(45deg)',
          border: `2px solid ${hexA(P.emberLit, (1 - hit) * 0.85)}`,
        }} />
      )}
      <div style={{
        position: 'absolute', left: HAND_X[1], top: SLOT.y, transform: 'translate(-50%,-50%)',
        opacity: mix(0.55, 1, MOTION.enter(seg(p, 0.4, 1))),
      }}>
        <CardFront d={ALLY} />
      </div>
      <Hand dim={1 - MOTION.enter(seg(p, 0.5, 1))} />
      <div style={{
        position: 'absolute', left: SLOT.x, top: SLOT.y, transform: 'translate(-50%,-50%)',
        filter: `drop-shadow(0 10px 22px rgba(0,0,0,.8)) drop-shadow(0 0 ${18 * relOut}px ${hexA(P.teal, 0.3 * relOut)})`,
      }}>
        <CardFront d={SPELL} charge={relOut * 0.6} />
      </div>
      <Thread from={{ x: SLOT.x, y: SLOT.y - 40 }} to={FOE} t={1} o={relOut * 0.5} />
      <Reticle x={FOE.x} y={FOE.y} lock={1} o={relOut} />
      {cfg.damage && (
        <div style={{
          position: 'absolute', left: FOE.x, top: FOE.y - 20 - rise * 74,
          transform: 'translate(-50%,-50%)',
          opacity: MOTION.enter(seg(p, 0.06, 0.2)) * (1 - MOTION.enter(seg(p, 0.66, 0.94))),
        }}>
          <span style={{
            font: `700 46px/1.2 'Cinzel',serif`, letterSpacing: '.04em', color: P.emberLit,
            textShadow: `0 0 26px ${hexA(P.ember, 0.9)}, 0 3px 12px rgba(0,0,0,.9)`, whiteSpace: 'nowrap',
          }}>DESTROYED</span>
        </div>
      )}
      <Label text="RESOLVED" o={MOTION.enter(seg(p, 0.44, 0.72)) * out} rise={mix(12, 0, MOTION.enter(seg(p, 0.44, 0.72)))} tone={P.good} />
      <div style={{ position: 'absolute', inset: 0, background: '#0A0705', opacity: MOTION.enter(seg(p, 0.92, 1)) }} />
    </>
  );
}

/* ================= root ================= */
function CardCast() {
  const { useTweaks, TweaksPanel, TweakSection, TweakSlider, TweakToggle } = window;
  const [t, setTweak] = useTweaks(window.TWEAK_DEFAULTS);
  cfg.reticle = t.reticle;
  cfg.damage = t.damage;
  return (
    <>
      <SceneStage width={W} height={H} scenes={window.OM_SCENES} playback={window.OM_PLAYBACK} bg="#0A0705">
        {{ Idle: SceneIdle, Lift: SceneLift, Activate: SceneActivate, Target: SceneTarget, Resolve: SceneResolve }}
      </SceneStage>
      <TweaksPanel>
        <TweakSection label="Targeting" />
        <TweakSlider label="Reticle strength" value={t.reticle} min={0} max={1} step={0.05}
                     onChange={(v) => setTweak('reticle', v)} />
        <TweakToggle label="Result callout" value={t.damage} onChange={(v) => setTweak('damage', v)} />
        <TweakSection label="Editing" />
        <TweakToggle label="Motion editor" value={t.motionEditor} onChange={(v) => setTweak('motionEditor', v)} />
      </TweaksPanel>
    </>
  );
}

window.CardCast = CardCast;
