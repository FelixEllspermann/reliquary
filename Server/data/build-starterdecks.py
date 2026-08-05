"""Erzeugt starterdecks.json — die fuenf Decks zur Auswahl beim ersten Start.

Wird von Hand gepflegt und geprueft: jede Karte muss existieren, jedes Deck genau
deckMinSize Karten haben, kein Name oefter als erlaubt vorkommen (Banlist zaehlt
mit), und das Extra Deck darf sein Limit nicht sprengen. Lieber hier krachen als
spaeter beim Spieler, der ein illegales Deck geschenkt bekommt.

    python build-starterdecks.py
"""
import io, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
cards = json.load(io.open(os.path.join(HERE, "cards-full.json"), encoding="utf-8"))["cards"]
rules = json.load(io.open(os.path.join(HERE, "rules.json"), encoding="utf-8"))
try:
    banlist = json.load(io.open(os.path.join(HERE, "banlist.json"), encoding="utf-8"))
except Exception:
    banlist = {}

MAX = rules.get("maxCopiesPerCard", 3)
SIZE = rules.get("deckMinSize", 40)
EXTRA_MAX = 20


def limit_of(name):
    """Erlaubte Kopien laut Banlist, sonst das normale Limit."""
    entry = banlist.get("limits") if isinstance(banlist, dict) else None
    if isinstance(entry, dict) and name in entry:
        return entry[name]
    if isinstance(banlist, dict) and name in banlist and isinstance(banlist[name], int):
        return banlist[name]
    return MAX


DECKS = [
    {
        "id": "assembly",
        "name": "Assembly of Oaths",
        "archetypes": "Mechination · Sacrilegion",
        "hero": "Lyra the Warden",
        "blurb": "Build cheap, then spend it.",
        "description":
            "Mechination makes bodies out of nothing: every part searches the next one by "
            "category, so the chain never runs the same way twice. Sacrilegion eats those "
            "bodies — its Sacraments demand monsters from BOTH sides of the field, which "
            "means the summon itself is the removal.\n\n"
            "The trick is that your material is worthless on purpose. Oathling summons "
            "itself for free, Pledgebearer comes back from the Graveyard, and Kilnwarden "
            "reprints Level 1s every turn. You never feed anything you'd miss.\n\n"
            "Slowest first turn in the set — there is nothing in your Graveyard to recycle "
            "yet. Survive two turns and the engine never stops.",
        "cards": {
            "Mechination Cogwright": 3, "Mechination Ratchet": 3, "Mechination Boltling": 3,
            "Mechination Spindle": 2, "Mechination Hammerhand": 2, "Mechination Kilnwarden": 2,
            "Mechination Blueprint": 3, "Mechination Recast": 2, "Mechination Assembly Line": 1,
            "Sacrilegion Acolyte": 3, "Sacrilegion Oathling": 3, "Sacrilegion Pledgebearer": 3,
            "Sacrilegion Herald": 2, "Sacrilegion Vowkeeper": 2, "Sacrilegion Rite of Return": 3,
            "Sacrilegion Sworn Oath": 2, "Sacrilegion Covenant Stone": 1,
        },
        "extra": {
            "Sacrilegion First Sacrament": 2, "Sacrilegion Second Sacrament": 2,
            "Sacrilegion Third Sacrament": 1, "Sacrilegion Broken Vow": 1,
            "Sacrilegion, the Last Oath": 1,
            "Mechination Assemblage": 2, "Mechination Worldgear": 1,
        },
    },
    {
        "id": "quiethand",
        "name": "The Quiet Hand",
        "archetypes": "Sleightwind · Manacle",
        "hero": "Lyra the Warden",
        "blurb": "Play on their turn, not yours.",
        "description":
            "This deck barely does anything in your own turn — and that is the point. "
            "Sleightwind monsters are discarded from your HAND during your opponent's turn "
            "to stop an attack, negate an effect, or bounce a monster. They never need to "
            "reach the field at all.\n\n"
            "Manacle works the other half: mana that is taken during your opponent's turn "
            "is gone, because they already drew their fill. The Coupled effects let you "
            "choose — take a little now, or cut their NEXT turn short before it starts.\n\n"
            "Choir of Two buys the discarded cards back, so you lose about half a card per "
            "round instead of a whole one. Meanwhile they keep not doing what they planned.",
        "cards": {
            "Sleightwind Whisperer": 3, "Sleightwind Doubtbringer": 3, "Sleightwind Maskbearer": 2,
            "Sleightwind Thornmother": 2, "Sleightwind Hush": 2, "Sleightwind Second Face": 3,
            "Manacle Tollkeeper": 3, "Manacle Gleaner": 3, "Manacle Coinbiter": 3,
            "Manacle Ledgerkeeper": 2, "Manacle Usurer": 2, "Manacle Debtwarden": 2,
            "Manacle Assessor": 2, "Manacle Bailiff": 2, "Manacle Levy": 3,
            "Manacle Reckoning": 2, "Manacle Countinghouse": 1,
        },
        "extra": {
            "Sleightwind Choir of Two": 2, "Sleightwind the Unwitnessed": 2,
            "Manacle Debt Collector": 2, "Manacle, the Final Ledger": 1,
        },
    },
    {
        "id": "kindle",
        "name": "Kindle and Ash",
        "archetypes": "Kindlekin · Gravemaw",
        "hero": "Ignis the Pyromancer",
        "blurb": "Many small things, then one very large one.",
        "description":
            "Six Level 1 beasts that pull each other onto the field for free. None of them "
            "is strong alone — the value is the count, and the count is also what your "
            "Reliquaries ask for.\n\n"
            "Gravemaw turns the losses into fuel: everything that dies feeds the Graveyard, "
            "and Kindlekin reads the Graveyard as a second hand.\n\n"
            "The payoff is Kindlekin, the Last Ember: four monsters on the field, six cards "
            "in the Graveyard, four Mana and two banished — and then it destroys every "
            "monster on the table except BEAST. Your whole swarm survives. Theirs does not, "
            "unless they happen to play Beasts too.",
        "cards": {
            "Kindlekin Spark": 3, "Kindlekin Ashling": 3, "Kindlekin Flickerpaw": 3,
            "Kindlekin Emberwing": 3, "Kindlekin Hearthnurse": 3, "Kindlekin Pyrewhelp": 3,
            "Kindlekin Tinderfall": 3,
            "Gravemaw Whelp": 3, "Gravemaw Butcher": 3, "Gravemaw Tyrant": 2,
            "Gravemaw Feast": 3, "Gravemaw Ossuary": 2,
            "Ember Spirit": 2, "Grave Call": 2, "Echo of the Fallen": 2,
        },
        "extra": {
            "Kindlekin Pyre Warden": 2, "Kindlekin Emberthrone": 2,
            "Kindlekin, the Last Ember": 1, "Gravemaw, the Bottomless": 2,
        },
    },
    {
        "id": "tide",
        "name": "Tide and Tempest",
        "archetypes": "Tidebound · Fethaerbreese",
        "hero": "Lyra the Warden",
        "blurb": "Give it back, take it again.",
        "description":
            "Water that returns and wind that never lands. Tidebound pulls cards back to "
            "hands and decks — including yours, on purpose — so the same good card gets "
            "played over and over.\n\n"
            "Fethaerbreese fills the gaps: cheap fliers that summon themselves while a "
            "\"Fethaerbreese\" monster is already out, and spells that make attacking into "
            "you a bad idea.\n\n"
            "The friendliest of the five to learn on. Nothing here demands a long setup, "
            "there is no hard combo to memorise, and a bad opening hand still plays out. "
            "If you have never played a card game like this, start here.",
        "cards": {
            "Tidebound Skimmer": 3, "Tidebound Current-Caller": 3, "Tidebound Mirrorshell": 3,
            "Tidebound Leviathan": 2, "Tidebound Undertow": 3,
            "Fethaerbreese Fledgling": 3, "Fethaerbreese Nightjar": 3,
            "Fethaerbreese Hollowbone": 3, "Fethaerbreese Nightmother": 2,
            "Fethaerbreese Updraft": 3, "Fethaerbreese Windless Hour": 2,
            "Frost Wolf": 3, "Mireback Toad": 2, "Rising Tide": 3, "Frost Nova": 2,
        },
        "extra": {
            "Tidebound, the Returning Sea": 2, "Fethaerbreese, the Held Breath": 2,
        },
    },
    {
        "id": "forge",
        "name": "Forge and Fang",
        "archetypes": "Forgeheart · Wyldpack",
        "hero": "Ignis the Pyromancer",
        "blurb": "Hit hard, hit again.",
        "description":
            "The straightforward one. Wyldpack swarms and grows — the more wolves you "
            "control, the harder each of them hits. Forgeheart hands out Artifacts that "
            "stay equipped, so a Level 1 body ends up swinging like a Level 3.\n\n"
            "There is almost no recursion and no denial. You put monsters down, you make "
            "them bigger, you attack. When it works it ends games two turns before anyone "
            "else's deck gets going.\n\n"
            "The weakness is honest: if your board gets wiped you have no engine to rebuild "
            "from. Trade early, keep a card in reserve, and do not overextend into a full "
            "opposing field.",
        "cards": {
            "Forgeheart Stoker": 3, "Forgeheart Anvilborn": 3, "Forgeheart Colossus": 2,
            "Forgeheart Bellows": 2, "Forgeheart Hammer": 2,
            "Wyldpack Cub": 3, "Wyldpack Howler": 3, "Wyldpack Stalker": 3,
            "Wyldpack Matriarch": 3, "Wyldpack Alpha": 2,
            "Call of the Wyld": 3, "Nimble Goblin": 3, "Spark Wolf": 3,
            "Heart of the Forge": 2, "Iron Resolve": 3,
        },
        "extra": {
            "Forgeheart Worldanvil": 2, "Wyldpack Ur-Alpha": 2,
        },
    },
]

problems = []
out = []
for deck in DECKS:
    main, extra = [], []
    for name, count in deck["cards"].items():
        if name not in cards:
            problems.append("%s: unbekannte Karte '%s'" % (deck["id"], name)); continue
        if cards[name]["class"] == "ReliquaryCardData":
            problems.append("%s: '%s' ist eine Reliquary und gehoert ins Extra Deck" % (deck["id"], name))
        allowed = limit_of(name)
        if count > allowed:
            problems.append("%s: %dx '%s' — erlaubt sind %d" % (deck["id"], count, name, allowed))
        main += [name] * count
    for name, count in deck["extra"].items():
        if name not in cards:
            problems.append("%s: unbekannte Extra-Karte '%s'" % (deck["id"], name)); continue
        if cards[name]["class"] != "ReliquaryCardData":
            problems.append("%s: '%s' ist keine Reliquary" % (deck["id"], name))
        if count > limit_of(name):
            problems.append("%s: %dx '%s' im Extra — zu viele" % (deck["id"], count, name))
        extra += [name] * count

    if len(main) != SIZE:
        problems.append("%s: %d Hauptdeck-Karten, verlangt sind %d" % (deck["id"], len(main), SIZE))
    if len(extra) > EXTRA_MAX:
        problems.append("%s: %d Extra-Karten, erlaubt sind %d" % (deck["id"], len(extra), EXTRA_MAX))
    if deck["hero"] not in cards or cards[deck["hero"]]["class"] != "PlayerCardData":
        problems.append("%s: '%s' ist keine Heldenkarte" % (deck["id"], deck["hero"]))

    out.append({
        "id": deck["id"], "name": deck["name"], "archetypes": deck["archetypes"],
        "blurb": deck["blurb"], "description": deck["description"],
        "hero": deck["hero"], "cards": main, "extra": extra,
    })
    print("%-11s %-22s %2d Haupt · %2d Extra · Held %s"
          % (deck["id"], deck["name"], len(main), len(extra), deck["hero"]))

if problems:
    print("\nPROBLEME:")
    for p in problems:
        print("  " + p)
    sys.exit(1)

io.open(os.path.join(HERE, "starterdecks.json"), "w", encoding="utf-8", newline="\n").write(
    json.dumps(out, ensure_ascii=False, indent=1))
print("\nstarterdecks.json geschrieben — 5 Decks, keine Beanstandung.")
