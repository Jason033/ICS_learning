#!/usr/bin/env python3
"""Independently parse generated PCAPs; verify checksums, fields, ranges, assets.
No network, no imports from the generator, no Wireshark required.
This does NOT verify GUI dissection, tcp.analysis labels or RTP playback.
"""
from pathlib import Path
import struct,socket,json,hashlib,collections
root=Path(__file__).resolve().parent
m=json.loads((root/'wireshark-maintenance-manifest.json').read_text())
def csum(b):
 if len(b)%2:b+=b'\0'
 acc=0
 for n in range(0,len(b),2):acc+=(b[n]<<8)|b[n+1]
 while acc>65535:acc=(acc&65535)+(acc>>16)
 return acc
parsed={}
for name,info in m['files'].items():
 raw=(root/name).read_bytes();assert hashlib.sha256(raw).hexdigest()==info['sha256']
 magic,major,minor,zone,sig,snap,link=struct.unpack('<IHHIIII',raw[:24]);assert(magic,major,minor,link)==(0xa1b2c3d4,2,4,1)
 off=24;rs=[];expected=[r for r in m['frames'] if r['file']==name]
 for n,ex in enumerate(expected,1):
  sec,us,cap,orig=struct.unpack('<IIII',raw[off:off+16]);off+=16;d=raw[off:off+cap];off+=cap
  assert cap<=orig and len(d)==cap and (cap,orig)==(ex['captured_len'],ex['original_len'])
  assert abs((sec-m['clock_base_epoch'])+us/1e6-ex['wire_time'])<1e-6
  assert d[12:14]==b'\x08\x00';ip=d[14:];ihl=(ip[0]&15)*4;plen=struct.unpack('!H',ip[2:4])[0];assert plen+14==orig
  assert csum(ip[:ihl])==65535
  src=socket.inet_ntoa(ip[12:16]);dst=socket.inet_ntoa(ip[16:20]);proto=ip[9];l4=ip[ihl:]
  assert (src,dst,proto)==(ex['src'],ex['dst'],ex['proto'])
  sp,dp=struct.unpack('!HH',l4[:4]);assert(sp,dp)==(ex['sport'],ex['dport'])
  if cap==orig:
   pseudo=ip[12:20]+struct.pack('!BBH',0,proto,len(l4));assert csum(pseudo+l4)==65535
  r=dict(t=ex['wire_time'],src=src,dst=dst,sport=sp,dport=dp,proto=proto)
  if proto==6:
   seq,ack=struct.unpack('!II',l4[4:12]);hl=(l4[12]>>4)*4;ln=plen-ihl-hl
   assert(seq,ack,ln,l4[13])==(ex['seq'],ex['ack'],ex['tcp_len'],ex['flags'])
   r.update(seq=seq,ack=ack,len=ln,flags=l4[13],payload=l4[hl:])
   if cap==orig and ln:
    payload=l4[hl:];declared=int.from_bytes(payload[:2],'big');assert declared==len(payload)-2
  else:
   ul=struct.unpack('!H',l4[4:6])[0];assert ul==plen-ihl
   payload=l4[8:];r.update(payload=payload)
   if cap==orig and 'rtp_seq' in ex:
    assert payload[0]>>6==2;seq,ts,ssrc=struct.unpack('!HII',payload[2:12]);assert(seq,ts,ssrc)==(ex['rtp_seq'],ex['rtp_ts'],ex['ssrc'])
    assert len(payload)==172;r.update(rtp_seq=seq,rtp_ts=ts,ssrc=ssrc)
   if cap==orig and 'call_id' in ex:
    h,b=payload.split(b'\r\n\r\n',1);fields=dict(line.split(b':',1) for line in h.split(b'\r\n')[1:])
    assert int(fields[b'Content-Length'].strip())==len(b)
    assert fields[b'Call-ID'].strip().decode()==ex['call_id']
    if b:assert b'm=audio ' in b and b'PCMU/8000' in b
  rs.append(r)
 assert off==len(raw) and len(rs)==info['frames'];parsed[name]=rs
 print(name, len(rs),'frames: structure, IPv4 and available transport checksums PASS')
rs=parsed['wireshark-maintenance-mixed.pcap'];ref=parsed['wireshark-maintenance-receiver.pcap']
norm=[r for r in rs if r['proto']==6 and 51000 in [r['sport'],r['dport']]]
assert len(norm)==10
for sp,seq,ack,ln in [(51000,1001,9001,13),(9100,9001,1014,15),(51000,1015,9017,0)]:assert any((r['sport'],r['seq'],r['ack'],r['len'])==(sp,seq,ack,ln) for r in norm)
for p,s in [(51003,4001),(51004,5001),(51005,6001),(51006,7001)]:
 req=[r for r in rs if r['proto']==6 and r['sport']==p and r['len']==13];assert req and all(r['seq']==s for r in req)
 assert any(r['proto']==6 and r['dport']==p and r['ack']==s+13 for r in rs)
inc=[r for r in rs if r.get('ssrc')==0x22222222];inref=[r for r in ref if r.get('ssrc')==0x22222222]
assert len(inc)==20 and len(set(r['rtp_seq'] for r in inc))==19 and 305 not in [r['rtp_seq'] for r in inc]
assert len(inref)==21 and len(set(r['rtp_seq'] for r in inref))==20
assert [r['rtp_seq'] for r in inc].index(309)<[r['rtp_seq'] for r in inc].index(308)
assert sum(r['proto']==6 and 51002 in [r['sport'],r['dport']] for r in rs)==3
print('PASS: TCP framing/seq/ack, SIP Content-Length, RTP counts/order, paired observation variation.')
