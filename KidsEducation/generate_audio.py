import argparse
import json
import os
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path


PROJECT_DIR = Path(__file__).resolve().parent


def load_dotenv() -> None:
    env_path = PROJECT_DIR / ".env"
    if not env_path.exists():
        return

    for raw_line in env_path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue

        key, value = line.split("=", 1)
        key = key.strip()
        value = value.strip().strip('"').strip("'")
        os.environ.setdefault(key, value)


load_dotenv()

API_KEY = os.getenv("ELEVENLABS_API_KEY", "").strip()
VOICE_FEMALE = os.getenv("ELEVENLABS_VOICE_FEMALE", "Hr3W7yWIljG9YBJn39oK")
VOICE_MALE = os.getenv("ELEVENLABS_VOICE_MALE", "xDppd78rqTGY8ICN7M4n")
MODEL_ID = os.getenv("ELEVENLABS_MODEL_ID", "eleven_multilingual_v2")
REQUEST_DELAY = float(os.getenv("ELEVENLABS_DELAY", "0.4"))

CONTENT_DIR = Path(os.getenv("KIDS_CONTENT_DIR", PROJECT_DIR / "Content"))
OUTPUT_DIR = Path(os.getenv("KIDS_AUDIO_DIR", PROJECT_DIR / "Resources" / "Raw" / "Audio"))


def item_key(item_id: str) -> str:
    return item_id.split("_", 1)[1] if "_" in item_id else item_id


def generate(text: str, voice_id: str, out_path: Path, *, overwrite: bool, dry_run: bool) -> str:
    if not text.strip():
        return "skip"

    if out_path.exists() and not overwrite:
        print(f"  [SKIP] {out_path.name}")
        return "skip"

    if dry_run:
        print(f"  [DRY]  {out_path.name} <- {text[:70]}")
        return "ok"

    if not API_KEY:
        print("  [ERR]  ELEVENLABS_API_KEY is missing. Set it in environment or .env.")
        return "err"

    url = f"https://api.elevenlabs.io/v1/text-to-speech/{voice_id}"
    headers = {"xi-api-key": API_KEY, "Content-Type": "application/json"}
    payload = {
        "text": text,
        "model_id": MODEL_ID,
        "voice_settings": {"stability": 0.6, "similarity_boost": 0.75},
    }

    try:
        body = json.dumps(payload).encode("utf-8")
        request = urllib.request.Request(url, data=body, headers=headers, method="POST")
        with urllib.request.urlopen(request, timeout=30) as response:
            content = response.read()

        out_path.parent.mkdir(parents=True, exist_ok=True)
        out_path.write_bytes(content)
        print(f"  [OK]   {out_path.name}")
        time.sleep(REQUEST_DELAY)
        return "ok"
    except urllib.error.HTTPError as exc:
        error_text = exc.read().decode("utf-8", errors="replace")
        print(f"  [ERR]  {out_path.name} -> {exc.code}: {error_text[:160]}")
        return "err"
    except Exception as exc:
        print(f"  [ERR]  {out_path.name} -> {exc}")
        return "err"


def content_files(selected_categories: set[str] | None) -> list[Path]:
    files = []
    for path in sorted(CONTENT_DIR.glob("*.json")):
        if path.name == "categories.json":
            continue

        try:
            data = json.loads(path.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            print(f"[SKIP] Invalid JSON: {path.name}")
            continue

        items = data.get("items", [])
        if not items:
            continue

        category_id = data.get("categoryId") or path.stem
        if selected_categories and category_id not in selected_categories and path.stem not in selected_categories:
            continue

        files.append(path)

    return files


def add_job(jobs: list[tuple[str, str, Path]], label: str, text: str | None, path: Path) -> None:
    if text and text.strip():
        jobs.append((label, text.strip(), path))


def build_jobs(item: dict, voices: str, include_male_descriptions: bool) -> list[tuple[str, str, Path]]:
    item_id = item.get("id", "").strip()
    name_tr = item.get("nameTr", "").strip()
    if not item_id or not name_tr:
        return []

    suffixes: list[tuple[str, str]] = []
    if voices in ("female", "both"):
        suffixes.append(("", VOICE_FEMALE))
    if voices in ("male", "both"):
        suffixes.append(("_m", VOICE_MALE))

    key = item_key(item_id)
    jobs: list[tuple[str, str, Path]] = []

    for suffix, _voice_id in suffixes:
        add_job(jobs, _voice_id, name_tr, OUTPUT_DIR / f"speech_tr_{key}{suffix}.mp3")

    clue_text = item.get("soundClueText", "")
    for suffix, _voice_id in suffixes:
        add_job(jobs, _voice_id, clue_text, OUTPUT_DIR / f"clue_{item_id}{suffix}.mp3")

    desc_text = item.get("descriptionTr", "")
    add_job(jobs, VOICE_FEMALE, desc_text, OUTPUT_DIR / f"{item_id}.mp3")
    if include_male_descriptions:
        add_job(jobs, VOICE_MALE, desc_text, OUTPUT_DIR / f"{item_id}_m.mp3")

    # AudioService.SpeakFunFactAsync currently looks for Audio/fact_{itemId}.mp3.
    fact_text = item.get("funFact", "")
    add_job(jobs, VOICE_FEMALE, fact_text, OUTPUT_DIR / f"fact_{item_id}.mp3")

    return jobs


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate KidsEducation audio files from Content/*.json.")
    parser.add_argument(
        "--categories",
        nargs="*",
        help="Optional category ids/files to generate, e.g. planets cities nature weather.",
    )
    parser.add_argument(
        "--voices",
        choices=["female", "male", "both"],
        default="both",
        help="Voices for item names and clues. Descriptions/facts are female unless noted.",
    )
    parser.add_argument("--overwrite", action="store_true", help="Regenerate files even if they already exist.")
    parser.add_argument("--dry-run", action="store_true", help="Print planned files without calling ElevenLabs.")
    parser.add_argument(
        "--include-male-descriptions",
        action="store_true",
        help="Also generate {item_id}_m.mp3 description files.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    selected = set(args.categories) if args.categories else None

    if not CONTENT_DIR.exists():
        print(f"[ERR] Content directory not found: {CONTENT_DIR}")
        return 1

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    totals = {"ok": 0, "skip": 0, "err": 0}
    files = content_files(selected)
    if not files:
        print("[INFO] No content files found for the selected categories.")
        return 0

    for json_path in files:
        data = json.loads(json_path.read_text(encoding="utf-8"))
        items = data.get("items", [])

        print(f"\n{'=' * 60}")
        print(f" {json_path.name} ({len(items)} item)")
        print(f"{'=' * 60}")

        for item in items:
            jobs = build_jobs(item, args.voices, args.include_male_descriptions)
            for voice_id, text, out_path in jobs:
                result = generate(text, voice_id, out_path, overwrite=args.overwrite, dry_run=args.dry_run)
                totals[result] += 1

    print(f"\n{'=' * 60}")
    print(f" DONE  ok={totals['ok']}  skipped={totals['skip']}  errors={totals['err']}")
    print(f" Output: {OUTPUT_DIR}")
    print(f"{'=' * 60}")

    return 1 if totals["err"] else 0


if __name__ == "__main__":
    sys.exit(main())
