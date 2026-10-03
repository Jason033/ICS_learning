"""Fixed, fictional timelines for learning reliability; no I/O or real delays."""

import argparse


CASES = {
    "timeout-late": [
        (0, "送出 START(id=7)"),
        (50, "本地等待到期：結果未知"),
        (70, "遠端完成 id=7；須核對權威狀態"),
    ],
    "timeout-no-delivery": [
        (0, "送出 START(id=8)"),
        (50, "本地等待到期：結果未知"),
        (70, "教學全知視角：遠端從未收到；本地在 50 ms 不知道"),
    ],
    "duplicate": [
        (0, "送出 START(id=9)，遠端執行一次"),
        (50, "本地等待到期"),
        (60, "重送 START(id=9)；只有遠端有去重契約才可避免第二次副作用"),
    ],
    "out-of-order": [
        (50, "收到 session=A seq=42 Busy"),
        (70, "收到 session=A seq=41 Ready；在同來源單調序號契約下屬舊事件"),
        (90, "收到 session=B seq=1 Ready；不可與 A 的序號直接比大小"),
    ],
}


def show_queue():
    print("\nqueue-growth：每秒進 8、出 5、容量 12；整秒模型")
    queued = 0
    for second in range(1, 7):
        available = queued + 8
        completed = min(available, 5)
        remaining = available - completed
        dropped = max(0, remaining - 12)
        queued = min(remaining, 12)
        print(f"第 {second} 秒：完成 {completed}，排隊 {queued}，本秒丟棄 {dropped}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--case", choices=[*CASES, "queue-growth", "all"], default="all")
    choice = parser.parse_args().case
    print("虛構教學資料；時間是虛擬毫秒，不是設備實測。")
    for name, events in CASES.items():
        if choice in (name, "all"):
            print(f"\n{name}")
            for ms, event in events:
                print(f"{ms:>3} ms  {event}")
    if choice in ("queue-growth", "all"):
        show_queue()


if __name__ == "__main__":
    main()
