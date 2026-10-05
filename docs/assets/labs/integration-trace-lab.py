"""Pure local event simulator for learning API trace boundaries; no network/device."""
import argparse
import heapq

CASES = ('success', 'timeout', 'late', 'duplicate', 'stale-session')


class DemoTrace:
    def __init__(self, case):
        self.case = case
        self.now = 0
        self.order = 0
        self.queue = []
        self.request_id = 'q17'
        self.session = 1
        self.result = 'pending'

    def schedule(self, at_ms, action):
        self.order += 1
        heapq.heappush(self.queue, (at_ms, self.order, action))

    def log(self, layer, message):
        print(f'{self.now:03d}ms {layer:<11} req={self.request_id} session={self.session} {message}')

    def view_click(self):
        self.log('View', 'CLICK GetStatus')
        self.controller_request()

    def controller_request(self):
        self.log('Controller', 'VALIDATED; waiting for device result')
        self.adapter_send()
        self.schedule(50, self.on_deadline)

    def adapter_send(self):
        self.log('Adapter', 'SEND GetStatus to fictional DemoCtrl-A')
        self.schedule(5, lambda: self.log('Adapter', 'ACK accepted; final state not confirmed'))
        if self.case in ('success', 'duplicate', 'stale-session'):
            self.schedule(20, lambda: self.on_device_event(self.request_id, 1))
        if self.case == 'late':
            self.schedule(80, lambda: self.on_device_event(self.request_id, 1))
        if self.case == 'duplicate':
            self.schedule(25, lambda: self.on_device_event(self.request_id, 1))
        if self.case == 'stale-session':
            self.schedule(12, self.reconnect)

    def reconnect(self):
        self.session = 2
        self.log('Controller', 'RECONNECTED; old-session events require verification')

    def on_device_event(self, event_request_id, event_session):
        self.log('FakeDevice', f'EVENT state=READY event_request={event_request_id} event_session={event_session}')
        if event_request_id != self.request_id:
            self.log('Controller', 'REQUEST_MISMATCH; do not apply to this operation')
        elif event_session != self.session:
            self.log('Controller', 'STALE_SESSION; do not apply to new session')
        elif self.result == 'pending':
            self.result = 'confirmed'
            self.log('Controller', 'CONFIRMED; View may display READY')
        elif self.result == 'unknown':
            self.result = 'confirmed'
            self.log('Controller', 'LATE_MATCHED_EVENT; same request/session resolves Unknown')
        elif self.result == 'confirmed':
            self.log('Controller', 'DUPLICATE; already confirmed')
        else:
            self.log('Controller', 'LATE_EVENT; verify current status before updating View')

    def on_deadline(self):
        if self.result == 'pending':
            self.result = 'unknown'
            self.log('Controller', 'TIMEOUT; remote result unknown')

    def run(self):
        print(f'\n=== {self.case} — 虛構 DemoCtrl-A，非實際公司 API ===')
        self.schedule(0, self.view_click)
        while self.queue:
            self.now, _, action = heapq.heappop(self.queue)
            action()
        print(f'本次觀測結論：{self.result}（只限這份本機模擬）')


def main():
    parser = argparse.ArgumentParser(description='本機模擬 View→Controller→Adapter→虛構設備事件。')
    parser.add_argument('--case', choices=('all', *CASES), default='all')
    args = parser.parse_args()
    for case in CASES if args.case == 'all' else (args.case,):
        DemoTrace(case).run()


if __name__ == '__main__':
    main()
