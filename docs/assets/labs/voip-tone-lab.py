"""Synthetic audio sampling lab: 8 kHz, 16-bit mono PCM. No microphone/network."""
import argparse
import math
import struct
import wave
from pathlib import Path

SAMPLE_RATE = 8000
DURATION_SECONDS = 1
FREQUENCY_HZ = 440
SAMPLE_WIDTH_BYTES = 2
FRAME_MS = 20


def make_pcm():
    samples = [round(0.25 * 32767 * math.sin(2 * math.pi * FREQUENCY_HZ * n / SAMPLE_RATE))
               for n in range(SAMPLE_RATE * DURATION_SECONDS)]
    return struct.pack('<' + 'h' * len(samples), *samples)


def main():
    parser = argparse.ArgumentParser(description='本機合成音訊取樣練習，不讀麥克風、不連網。')
    parser.add_argument('--write-wav', action='store_true', help='在目前資料夾寫入 voip-tone-lab.wav；不覆寫既有檔案')
    args = parser.parse_args()
    pcm = make_pcm()
    samples_per_frame = SAMPLE_RATE * FRAME_MS // 1000
    raw_bytes_per_frame = samples_per_frame * SAMPLE_WIDTH_BYTES
    assert len(pcm) == 16000 and samples_per_frame == 160 and raw_bytes_per_frame == 320
    print(f'取樣：{SAMPLE_RATE} samples/s × {DURATION_SECONDS} s = {len(pcm)//SAMPLE_WIDTH_BYTES} samples')
    print(f'原始 PCM：{len(pcm)} bytes／秒 = {len(pcm)*8//1000} kbit/s')
    print(f'{FRAME_MS} ms：{samples_per_frame} samples，16-bit PCM {raw_bytes_per_frame} bytes')
    print('G.711 的 20 ms 語音 payload 約 160 bytes；此腳本沒有實作 G.711 編碼或 RTP。')
    if args.write_wav:
        target = Path('voip-tone-lab.wav')
        with target.open('xb') as file:
            with wave.open(file, 'wb') as output:
                output.setnchannels(1)
                output.setsampwidth(SAMPLE_WIDTH_BYTES)
                output.setframerate(SAMPLE_RATE)
                output.writeframes(pcm)
        print(f'已寫出：{target.resolve()}（合成 440 Hz 教學音，不含真人聲音）')


if __name__ == '__main__':
    main()
