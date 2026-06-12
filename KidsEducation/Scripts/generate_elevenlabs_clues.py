#!/usr/bin/env python3
"""Generate Sound Guess clue MP3 files with ElevenLabs.

Reads Content/*.json items that include `soundClueText` and writes files like:
Resources/Raw/Audio/clue_animal_kedi.mp3
Resources/Raw/Audio/clue_animal_kedi_m.mp3

Required environment variables:
  ELEVENLABS_API_KEY
"""

from __future__ import annotations

import argparse
import json
import os
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path


DEFAULT_MODEL_ID = "eleven_multilingual_v2"
DEFAULT_OUTPUT_FORMAT = "mp3_44100_128"
DEFAULT_GIRL_VOICE_ID = "Hr3W7yWIljG9YBJn39oK"
DEFAULT_BOY_VOICE_ID = "xDppd78rqTGY8ICN7M4n"
SKIP_JSON = {"categories.json", "songs.json", "tsconfig1.json"}


def repo_root_from_script() -> Path:
    return Path(__file__).resolve().parents[1]


def iter_items(content_dir: Path, category: str | None):
    for path in sorted(content_dir.glob("*.json")):
        if path.name in SKIP_JSON:
            continue
        if category and path.stem != category:
            continue

        data = json.loads(path.read_text(encoding="utf-8"))
        for item in data.get("items", []):
            clue = (item.get("soundClueText") or "").strip()
            item_id = (item.get("id") or "").strip()
            if item_id and clue:
                yield path.stem, item_id, clue


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
            "stability": 0.62,
            "similarity_boost": 0.78,
            "style": 0.15,
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
    parser = argparse.ArgumentParser()
    parser.add_argument("--girl-voice-id", default=os.getenv("ELEVENLABS_GIRL_VOICE_ID", DEFAULT_GIRL_VOICE_ID))
    parser.add_argument("--boy-voice-id", default=os.getenv("ELEVENLABS_BOY_VOICE_ID", DEFAULT_BOY_VOICE_ID))
    parser.add_argument("--voice-id", default=os.getenv("ELEVENLABS_VOICE_ID"), help="Generate only one custom voice.")
    parser.add_argument("--single-voice", action="store_true", help="Use --voice-id and generate only clue_{item}.mp3.")
    parser.add_argument("--api-key", default=os.getenv("ELEVENLABS_API_KEY"))
    parser.add_argument("--model-id", default=os.getenv("ELEVENLABS_MODEL_ID", DEFAULT_MODEL_ID))
    parser.add_argument("--output-format", default=DEFAULT_OUTPUT_FORMAT)
    parser.add_argument("--category", help="Only generate one content category, e.g. animals")
    parser.add_argument("--overwrite", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--limit", type=int)
    parser.add_argument("--timeout", type=int, default=60)
    parser.add_argument("--delay", type=float, default=0.25)
    args = parser.parse_args()

    if not args.api_key and not args.dry_run:
        raise SystemExit("ELEVENLABS_API_KEY is missing.")
    voices = [(args.girl_voice_id, ""), (args.boy_voice_id, "_m")]
    if args.single_voice:
        if not args.voice_id:
            raise SystemExit("ELEVENLABS_VOICE_ID is missing.")
        voices = [(args.voice_id, "")]

    root = repo_root_from_script()
    content_dir = root / "Content"
    audio_dir = root / "Resources" / "Raw" / "Audio"
    audio_dir.mkdir(parents=True, exist_ok=True)

    items = list(iter_items(content_dir, args.category))
    if args.limit:
        items = items[: args.limit]

    print(f"Found {len(items)} clue(s), {len(voices)} voice(s).")
    for index, (_category, item_id, clue) in enumerate(items, start=1):
        for voice_id, suffix in voices:
            target = audio_dir / f"clue_{item_id}{suffix}.mp3"
            if target.exists() and not args.overwrite:
                print(f"[{index}/{len(items)}] skip existing {target.name}")
                continue

            print(f"[{index}/{len(items)}] {target.name}: {clue}")
            if args.dry_run:
                continue

            try:
                audio = create_speech(
                    api_key=args.api_key,
                    voice_id=voice_id,
                    text=clue,
                    model_id=args.model_id,
                    output_format=args.output_format,
                    timeout=args.timeout,
                )
            except urllib.error.HTTPError as exc:
                detail = exc.read().decode("utf-8", errors="replace")
                raise SystemExit(f"ElevenLabs error for {item_id}: HTTP {exc.code}\n{detail}") from exc

            target.write_bytes(audio)
            time.sleep(args.delay)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
