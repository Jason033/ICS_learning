"""Verify public teaching PCAPs without third-party packet libraries."""
from pathlib import Path
import ipaddress
import struct

LABS = Path(__file__).resolve().parents[1] / 'docs/assets/labs'
ADDRESS_SET = {'192.0.2.10', '192.0.2.53', '198.51.100.20'}


def checksum(data: bytes) -> int:
    if len(data) & 1:
        data += b'\0'
    value = sum(struct.unpack('!' + 'H' * (len(data) // 2), data))
    while value >> 16:
        value = (value & 0xffff) + (value >> 16)
    return (~value) & 0xffff


def parse(path: Path) -> list[dict]:
    raw = path.read_bytes()
    assert len(raw) >= 24 and struct.unpack_from('<IHHIIII', raw, 0) == (
        0xa1b2c3d4, 2, 4, 0, 0, 65535, 1), path
    offset, previous = 24, -1
    packets = []
    while offset < len(raw):
        sec, micro, saved, original = struct.unpack_from('<IIII', raw, offset)
        offset += 16
        assert saved == original and 0 < saved <= 65535 and offset + saved <= len(raw)
        time = sec * 1_000_000 + micro
        assert 0 <= micro < 1_000_000 and time >= previous
        previous = time
        frame = raw[offset:offset + saved]
        offset += saved
        assert frame[12:14] == b'\x08\x00'
        ip = frame[14:]
        assert ip[0] == 0x45 and checksum(ip[:20]) == 0
        total = struct.unpack_from('!H', ip, 2)[0]
        assert total == len(ip)
        protocol = ip[9]
        src, dst = (str(ipaddress.IPv4Address(ip[pos:pos + 4])) for pos in (12, 16))
        assert src in ADDRESS_SET and dst in ADDRESS_SET
        segment = ip[20:]
        pseudo = ip[12:20] + struct.pack('!BBH', 0, protocol, len(segment))
        assert checksum(pseudo + segment) == 0, (path.name, len(packets) + 1)
        sport, dport = struct.unpack_from('!HH', segment)
        if protocol == 17:
            assert struct.unpack_from('!H', segment, 4)[0] == len(segment)
            payload = segment[8:]
            seq = None
        elif protocol == 6:
            header_len = (segment[12] >> 4) * 4
            assert header_len >= 20
            payload = segment[header_len:]
            seq = struct.unpack_from('!I', segment, 4)[0]
        else:
            raise AssertionError(f'unexpected protocol {protocol}')
        packets.append({'src': src, 'dst': dst, 'protocol': protocol,
                        'sport': sport, 'dport': dport, 'payload': payload,
                        'seq': seq})
    assert offset == len(raw)
    return packets


first = parse(LABS / 'wireshark-first-capture.pcap')
assert len(first) == 13
assert sum(p['protocol'] == 17 for p in first) == 2
assert sum(p['protocol'] == 6 for p in first) == 11
assert b'lab.example' not in b''.join(p['payload'] for p in first[:2])  # DNS wire format
assert b'GET /status' in first[5]['payload']
assert b'HTTP/1.1 200 OK' in first[7]['payload']

cases = parse(LABS / 'wireshark-tcp-cases.pcap')
assert len(cases) == 8
assert sum(p['dport'] == 7000 for p in cases) == 3
assert {p['seq'] for p in cases[:3]} == {10000}
assert b'STATUS?\n' in cases[6]['payload']
assert cases[7]['payload'] == b''

voip = parse(LABS / 'wireshark-sip-rtp.pcap')
assert len(voip) == 16
sip = [p for p in voip if p['sport'] == 5060 and p['dport'] == 5060]
rtp = [p for p in voip if {p['sport'], p['dport']} == {40000, 40002}]
assert len(sip) == 7 and len(rtp) == 9
assert b'INVITE' in sip[0]['payload'] and b'm=audio 40000' in sip[0]['payload']
assert b'm=audio 40002' in sip[3]['payload']
assert {struct.unpack_from('!H', p['payload'], 2)[0] for p in rtp if p['src'] == '192.0.2.10'} == set(range(100, 105))
assert {struct.unpack_from('!H', p['payload'], 2)[0] for p in rtp if p['src'] == '198.51.100.20'} == {300, 301, 303, 304}
print('通過：3 份教學 PCAP 的格式、時間、IP/TCP/UDP checksum、DNS/HTTP/SIP/RTP 案例。')
