#!/usr/bin/env python3
"""Generate tale page MP3 files with ElevenLabs.

Reads Content/tales.json and writes files like:
  Resources/Raw/Audio/tale_uc_keci_1.mp3
  Resources/Raw/Audio/tale_krmz_1.mp3
  ...

Usage:
  python Scripts/generate_tale_audio.py
  python Scripts/generate_tale_audio.py --tale-id uc_keci
  python Scripts/generate_tale_audio.py --dry-run
  python Scripts/generate_tale_audio.py --overwrite
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

# Masal seslendirmesi için çocuk dostu, sıcak bir ses
# Eski scriptten alınan ses ID'leri:
DEFAULT_VOICE_ID = "Hr3W7yWIljG9YBJn39oK"   # girl voice — masal seslendirme
DEFAULT_MODEL_ID = "eleven_multilingual_v2"
DEFAULT_OUTPUT_FORMAT = "mp3_44100_128"


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
            "stability": 0.70,
            "similarity_boost": 0.75,
            "style": 0.25,           # biraz anlatıcı tonu
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
    parser = argparse.ArgumentParser(description="Generate tale audio with ElevenLabs")
    parser.add_argument("--api-key", default=os.getenv("ELEVENLABS_API_KEY"))
    parser.add_argument("--voice-id", default=os.getenv("ELEVENLABS_VOICE_ID", DEFAULT_VOICE_ID))
    parser.add_argument("--model-id", default=DEFAULT_MODEL_ID)
    parser.add_argument("--output-format", default=DEFAULT_OUTPUT_FORMAT)
    parser.add_argument("--tale-id", help="Sadece belirli bir masalı üret (örn: uc_keci)")
    parser.add_argument("--overwrite", action="store_true", help="Var olan dosyaların üstüne yaz")
    parser.add_argument("--dry-run", action="store_true", help="API çağrısı yapmadan listele")
    parser.add_argument("--delay", type=float, default=0.4, help="API çağrıları arası bekleme (saniye)")
    parser.add_argument("--timeout", type=int, default=60)
    args = parser.parse_args()

    if not args.api_key and not args.dry_run:
        raise SystemExit("ELEVENLABS_API_KEY eksik. .env dosyasını kontrol et.")

    script_dir = Path(__file__).resolve().parent
    root = script_dir.parent
    tales_path = root / "Content" / "tales.json"
    audio_dir = root / "Resources" / "Raw" / "Audio"
    audio_dir.mkdir(parents=True, exist_ok=True)

    tales = json.loads(tales_path.read_text(encoding="utf-8-sig"))

    # Üretilecek (dosya_adı, metin) listesi
    tasks: list[tuple[str, str]] = []
    for tale in tales:
        if args.tale_id and tale["id"] != args.tale_id:
            continue
        for page in tale["pages"]:
            audio_file = page["audioFile"]   # örn: tale_uc_keci_1.mp3
            text = page["text"]
            tasks.append((audio_file, text))

    print(f"Toplam {len(tasks)} sayfa sesi üretilecek.")
    print(f"Ses ID: {args.voice_id}")
    print(f"Çıktı: {audio_dir}\n")

    generated = 0
    skipped = 0

    for i, (filename, text) in enumerate(tasks, start=1):
        target = audio_dir / filename
        if target.exists() and not args.overwrite:
            print(f"[{i:>2}/{len(tasks)}] atla (var)  {filename}")
            skipped += 1
            continue

        print(f"[{i:>2}/{len(tasks)}] üret       {filename}")
        print(f"           {text[:80]}{'…' if len(text) > 80 else ''}")

        if args.dry_run:
            continue

        try:
            audio = create_speech(
                api_key=args.api_key,
                voice_id=args.voice_id,
                text=text,
                model_id=args.model_id,
                output_format=args.output_format,
                timeout=args.timeout,
            )
        except urllib.error.HTTPError as exc:
            detail = exc.read().decode("utf-8", errors="replace")
            raise SystemExit(f"\nElevenLabs hatası ({filename}): HTTP {exc.code}\n{detail}") from exc

        target.write_bytes(audio)
        generated += 1
        time.sleep(args.delay)

    print(f"\nBitti! Üretilen: {generated}, Atlanan: {skipped}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
