"""Deterministic lost-update demonstration, followed by Lock protection."""

from threading import Barrier, Lock, Thread


def without_lock():
    shared = {"count": 0}
    read_together = Barrier(2)
    results = []

    def worker():
        old = shared["count"]
        read_together.wait()
        shared["count"] = old + 1
        results.append(old)

    workers = [Thread(target=worker, name=name) for name in ("A", "B")]
    for worker in workers:
        worker.start()
    for worker in workers:
        worker.join()
    assert sorted(results) == [0, 0]
    assert shared["count"] == 1
    return shared["count"]


def with_lock():
    shared = {"count": 0}
    guard = Lock()

    def worker():
        with guard:
            old = shared["count"]
            shared["count"] = old + 1

    workers = [Thread(target=worker, name=name) for name in ("A", "B")]
    for worker in workers:
        worker.start()
    for worker in workers:
        worker.join()
    assert shared["count"] == 2
    return shared["count"]


if __name__ == "__main__":
    print("虛構教學交錯；Barrier 固定兩個工作都先讀到 0，不是效能測試。")
    print("未保護，兩次加一後 count =", without_lock())
    print("Lock 保護讀改寫後 count =", with_lock())
