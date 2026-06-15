#!/usr/bin/env python3
"""Generate missing KidsEducation audio files with ElevenLabs.

Default behavior is conservative: existing healthy files are skipped.

Examples:
  python Scripts/generate_missing_audio.py --dry-run
  python Scripts/generate_missing_audio.py --types description,speech,clue --category traffic
  python Scripts/generate_missing_audio.py --types fact --limit 20
"""

from __future__ import annotations

import argparse
import json
import os
import time
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from pathlib import Path


DEFAULT_MODEL_ID = "eleven_multilingual_v2"
DEFAULT_OUTPUT_FORMAT = "mp3_44100_128"
DEFAULT_GIRL_VOICE_ID = "Hr3W7yWIljG9YBJn39oK"
DEFAULT_BOY_VOICE_ID = "xDppd78rqTGY8ICN7M4n"
SKIP_JSON = {"categories.json", "songs.json", "stories.json", "tales.json", "tsconfig1.json"}


@dataclass(frozen=True)
class AudioTask:
    filename: str
    text: str
    voice_id: str
    kind: str
    item_id: str


def repo_root_from_script() -> Path:
    return Path(__file__).resolve().parents[1]


def load_env_file(root: Path) -> None:
    env_path = root / ".env"
    if not env_path.exists():
        return

    for raw_line in env_path.read_text(encoding="utf-8-sig").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue

        key, value = line.split("=", 1)
        key = key.strip()
        value = value.strip().strip('"').strip("'")
        if key and key not in os.environ:
            os.environ[key] = value


def iter_items(content_dir: Path, category: str | None):
    for path in sorted(content_dir.glob("*.json")):
        if path.name in SKIP_JSON:
            continue
        if category and path.stem != category:
            continue

        data = json.loads(path.read_text(encoding="utf-8-sig"))
        for item in data.get("items", []):
            item_id = (item.get("id") or "").strip()
            if item_id:
                yield path.stem, item_id, item


def speech_key(item_id: str) -> str:
    return item_id.split("_", 1)[1] if "_" in item_id else item_id


def description_text(item: dict) -> str:
    name = (item.get("nameTr") or "").strip()
    desc = (item.get("descriptionTr") or "").strip()
    fact = (item.get("funFact") or "").strip()
    english = (item.get("nameEn") or "").strip()

    if desc:
        return f"{name}. {desc}" if name else desc
    if fact:
        return f"{name}. {fact}" if name else fact
    if english:
        return f"{name}. İngilizcesi {english}."
    return name


def clue_text(item: dict) -> str:
    clue = (item.get("soundClueText") or "").strip()
    if clue:
        return clue

    desc = description_text(item)
    return f"{desc} Acaba bu nedir?"


def fact_text(item: dict) -> str:
    fact = (item.get("funFact") or "").strip()
    name = (item.get("nameTr") or "").strip()
    if fact:
        return fact
    return f"{name} hakkında kısa ve eğlenceli bir bilgi."


def build_tasks(
    *,
    content_dir: Path,
    category: str | None,
    requested_types: set[str],
    girl_voice_id: str,
    boy_voice_id: str,
    single_voice: bool,
) -> list[AudioTask]:
    tasks: list[AudioTask] = []

    for _category, item_id, item in iter_items(content_dir, category):
        name = (item.get("nameTr") or "").strip()
        if not name:
            continue

        if "description" in requested_types:
            tasks.append(AudioTask(f"{item_id}.mp3", description_text(item), girl_voice_id, "description", item_id))

        if "speech" in requested_types:
            key = speech_key(item_id)
            tasks.append(AudioTask(f"speech_tr_{key}.mp3", name, girl_voice_id, "speech", item_id))
            if not single_voice:
                tasks.append(AudioTask(f"speech_tr_{key}_m.mp3", name, boy_voice_id, "speech", item_id))

        if "clue" in requested_types:
            tasks.append(AudioTask(f"clue_{item_id}.mp3", clue_text(item), girl_voice_id, "clue", item_id))
            if not single_voice:
                tasks.append(AudioTask(f"clue_{item_id}_m.mp3", clue_text(item), boy_voice_id, "clue", item_id))

        if "fact" in requested_types:
            tasks.append(AudioTask(f"fact_{item_id}.mp3", fact_text(item), girl_voice_id, "fact", item_id))

    return tasks


def create_speech(
    *,
    api_key: str,
    voice_id: str,
    text: str,
    model_id: str,
    output_format: str,
    timeout: int,
) -> bytes:
    query = urllib.parse.urlencode({"output_format": output_format})
    url = f"https://api.elevenlabs.io/v1/text-to-speech/{voice_id}?{query}"
    payload = {
        "text": text,
        "model_id": model_id,
        "language_code": "tr",
        "voice_settings": {
            "stability": 0.64,
            "similarity_boost": 0.78,
            "style": 0.16,
            "use_speaker_boost": True,
        },
    }

    request = urllib.request.Request(
        url,
        data=json.dumps(payload, ensure_ascii=False).encode("utf-8"),
        headers={
            "xi-api-key": api_key,
            "Content-Type": "application/json",
            "Accept": "audio/mpeg",
        },
        method="POST",
    )

    with urllib.request.urlopen(request, timeout=timeout) as response:
        return response.read()


def main() -> int:
    root = repo_root_from_script()
    load_env_file(root)

    parser = argparse.ArgumentParser(description="Generate missing KidsEducation audio files.")
    parser.add_argument("--api-key", default=os.getenv("ELEVENLABS_API_KEY"))
    parser.add_argument("--girl-voice-id", default=os.getenv("ELEVENLABS_GIRL_VOICE_ID", DEFAULT_GIRL_VOICE_ID))
    parser.add_argument("--boy-voice-id", default=os.getenv("ELEVENLABS_BOY_VOICE_ID", DEFAULT_BOY_VOICE_ID))
    parser.add_argument("--model-id", default=os.getenv("ELEVENLABS_MODEL_ID", DEFAULT_MODEL_ID))
    parser.add_argument("--output-format", default=DEFAULT_OUTPUT_FORMAT)
    parser.add_argument("--types", default="description,speech,clue", help="Comma list: description,speech,clue,fact")
    parser.add_argument("--category", help="Only one category, e.g. traffic")
    parser.add_argument("--single-voice", action="store_true", help="Generate only non-male files.")
    parser.add_argument("--overwrite", action="store_true", help="Overwrite existing files.")
    parser.add_argument("--min-bytes", type=int, default=1024, help="Treat smaller existing files as missing.")
    parser.add_argument("--dry-run", action="store_true", help="List work without calling ElevenLabs.")
    parser.add_argument("--limit", type=int)
    parser.add_argument("--timeout", type=int, default=60)
    parser.add_argument("--delay", type=float, default=0.3)
    args = parser.parse_args()

    requested_types = {part.strip() for part in args.types.split(",") if part.strip()}
    valid_types = {"description", "speech", "clue", "fact"}
    unknown_types = requested_types - valid_types
    if unknown_types:
        raise SystemExit(f"Unknown type(s): {', '.join(sorted(unknown_types))}")

    if not args.api_key and not args.dry_run:
        raise SystemExit("ELEVENLABS_API_KEY is missing. Add it to .env or pass --api-key.")

    audio_dir = root / "Resources" / "Raw" / "Audio"
    audio_dir.mkdir(parents=True, exist_ok=True)

    tasks = build_tasks(
        content_dir=root / "Content",
        category=args.category,
        requested_types=requested_types,
        girl_voice_id=args.girl_voice_id,
        boy_voice_id=args.boy_voice_id,
        single_voice=args.single_voice,
    )

    missing: list[AudioTask] = []
    skipped = 0
    for task in tasks:
        target = audio_dir / task.filename
        healthy = target.exists() and target.stat().st_size >= args.min_bytes
        if healthy and not args.overwrite:
            skipped += 1
            continue
        missing.append(task)

    if args.limit:
        missing = missing[: args.limit]

    print(f"Total task candidates: {len(tasks)}")
    print(f"Skipped existing: {skipped}")
    print(f"To generate: {len(missing)}")
    print(f"Output: {audio_dir}")

    generated = 0
    for index, task in enumerate(missing, start=1):
        target = audio_dir / task.filename
        print(f"[{index}/{len(missing)}] {task.kind}: {task.filename} ({task.item_id})")
        print(f"  {task.text[:120]}{'...' if len(task.text) > 120 else ''}")

        if args.dry_run:
            continue

        try:
            audio = create_speech(
                api_key=args.api_key,
                voice_id=task.voice_id,
                text=task.text,
                model_id=args.model_id,
                output_format=args.output_format,
                timeout=args.timeout,
            )
        except urllib.error.HTTPError as exc:
            detail = exc.read().decode("utf-8", errors="replace")
            raise SystemExit(f"ElevenLabs error for {task.filename}: HTTP {exc.code}\n{detail}") from exc

        target.write_bytes(audio)
        generated += 1
        time.sleep(args.delay)

    print(f"Done. Generated: {generated}, skipped existing: {skipped}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
