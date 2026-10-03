#!/usr/bin/env python3
"""Deterministic fictional offline Ethernet PCAPs. No network or dependencies.
Run in an empty directory. Creates three PCAPs, manifest and application logs.
"""
from pathlib import Path
import struct,socket,json,csv,hashlib
BASE=1760000000;A='192.0.2.10';B='198.51.100.20';C='192.0.2.30';records=[]
def checksum(b):
 if len(b)%2:b+=b'\0'
 s=sum(struct.unpack('!%dH'%(len(b)//2),b))
 while s>>16:s=(s&65535)+(s>>16)
 return (~s)&65535

def packet(t,scenario,src,dst,proto,payload,**fields):
 h=struct.pack('!BBHHHBBH4s4s',0x45,0,len(payload)+20,len(records)+1,0,64,proto,0,socket.inet_aton(src),socket.inet_aton(dst))
 h=h[:10]+struct.pack('!H',checksum(h))+h[12:]
 records.append(dict(t=t,scenario=scenario,src=src,dst=dst,proto=proto,data=bytes.fromhex('0200000000020200000000010800')+h+payload,**fields))

def tcp(t,scenario,sport,dport,seq,ack,flags,payload=b'',src=A,dst=B,window=4096):
 h=struct.pack('!HHIIBBHHH',sport,dport,seq,ack,0x50,flags,window,0,0)
 pseudo=socket.inet_aton(src)+socket.inet_aton(dst)+struct.pack('!BBH',0,6,len(h)+len(payload))
 h=h[:16]+struct.pack('!H',checksum(pseudo+h+payload))+h[18:]
 packet(t,scenario,src,dst,6,h+payload,sport=sport,dport=dport,seq=seq,ack=ack,flags=flags,payload_hex=payload.hex(),tcp_len=len(payload),window=window)

def udp(t,scenario,sport,dport,payload,src=A,dst=B,**f):
 h=struct.pack('!HHHH',sport,dport,len(payload)+8,0)
 pseudo=socket.inet_aton(src)+socket.inet_aton(dst)+struct.pack('!BBH',0,17,len(h)+len(payload))
 h=h[:6]+struct.pack('!H',checksum(pseudo+h+payload) or 65535)
 packet(t,scenario,src,dst,17,h+payload,sport=sport,dport=dport,payload_hex=payload.hex(),**f)

def handshake(t,s,p,initial=1000):
 tcp(t,s,p,9100,initial,0,2);tcp(t+.010,s,9100,p,9000,initial+1,0x12,src=B,dst=A);tcp(t+.020,s,p,9100,initial+1,9001,0x10)
handshake(.1,'normal',51000)
tcp(.130,'normal',51000,9100,1001,9001,0x18,b'\x00\x0bREQ q17 GET')
tcp(.140,'normal',9100,51000,9001,1014,0x10,src=B,dst=A)
tcp(.170,'normal',9100,51000,9001,1014,0x18,b'\x00\x0dRSP q17 READY',src=B,dst=A)
tcp(.180,'normal',51000,9100,1014,9016,0x10)
tcp(.190,'normal',51000,9100,1014,9016,0x11)
tcp(.200,'normal',9100,51000,9016,1015,0x11,src=B,dst=A)
tcp(.210,'normal',51000,9100,1015,9017,0x10)
tcp(.250,'refused',51001,9100,2000,0,2);tcp(.260,'refused',9100,51001,0,2001,0x14,src=B,dst=A)
for t in [.300,1.300,3.300]:tcp(t,'syn-only',51002,9100,3000,0,2)
handshake(.4,'ack-no-response',51003,4000)
tcp(.430,'ack-no-response',51003,9100,4001,9001,0x18,b'\x00\x0bREQ q18 GET')
tcp(.440,'ack-no-response',9100,51003,9001,4014,0x10,src=B,dst=A)
handshake(.5,'retransmission',51004,5000)
for t in [.530,.830]:tcp(t,'retransmission',51004,9100,5001,9001,0x18,b'\x00\x0bREQ q19 GET')
tcp(.840,'retransmission',9100,51004,9001,5014,0x10,src=B,dst=A)
tcp(.850,'retransmission',9100,51004,9001,5014,0x18,b'\x00\x0dRSP q19 READY',src=B,dst=A)
tcp(.860,'retransmission',51004,9100,5014,9016,0x10)
handshake(.6,'capture-omission',51005,6000)
tcp(.630,'capture-omission',51005,9100,6001,9001,0x18,b'\x00\x0bREQ q20 GET')
tcp(.640,'capture-omission',9100,51005,9001,6014,0x10,src=B,dst=A)
tcp(.650,'capture-omission',9100,51005,9001,6014,0x18,b'\x00\x0dRSP q20 READY',src=B,dst=A)
tcp(.660,'capture-omission',51005,9100,6014,9016,0x10)
handshake(.7,'zero-window',51006,7000)
tcp(.730,'zero-window',51006,9100,7001,9001,0x18,b'\x00\x0bREQ q21 GET')
tcp(.740,'zero-window',9100,51006,9001,7014,0x10,src=B,dst=A,window=0)
tcp(1.040,'zero-window',9100,51006,9001,7014,0x10,src=B,dst=A,window=4096)
for i in range(60):
 dns=struct.pack('!HHHHHH',0x1200+i,0x0100,1,0,0,0)+b'\x03lab\x07example\0'+struct.pack('!HH',1,1)
 udp(.015+i*.05,'dns-noise',53000+i,53,dns,src=C,dst='192.0.2.53')
for i in range(15):udp(.025+i*.2,'telemetry-noise',62000,62001,('TEMP=%02d'%i).encode(),src=C,dst='192.0.2.40')
def sip(t,call,start):
 cid=f'lab-{call}@example.invalid';branch=f'z9hG4bK-{call}'
 offer=f'v=0\r\no=- 1 1 IN IP4 {A}\r\ns=Lab\r\nc=IN IP4 {A}\r\nt=0 0\r\nm=audio {start} RTP/AVP 0\r\na=rtpmap:0 PCMU/8000\r\na=sendrecv\r\n';answer=offer.replace(A,B).replace(str(start),str(start+2))
 def msg(first,method,body='',totag=False):
  h=f'{first}\r\nVia: SIP/2.0/UDP {A}:5060;branch={branch if method != "ACK" else branch + "-ack"}\r\nFrom: <sip:a@example.invalid>;tag=a{call}\r\nTo: <sip:b@example.invalid>'+ (f';tag=b{call}' if totag else '')+f'\r\nCall-ID: {cid}\r\nCSeq: 1 {method}\r\n'
  h += (f'Max-Forwards: 70\r\n' if not first.startswith('SIP/') else '') + f'Contact: <sip:{"b" if first.startswith("SIP/") else "a"}@{B if first.startswith("SIP/") else A}:5060>\r\n'
  return (h+('Content-Type: application/sdp\r\n' if body else '')+f'Content-Length: {len(body.encode())}\r\n\r\n'+body).encode()
 udp(t,'sip-'+call,5060,5060,msg('INVITE sip:b@example.invalid SIP/2.0','INVITE',offer),call_id=cid)
 udp(t+.01,'sip-'+call,5060,5060,msg('SIP/2.0 180 Ringing','INVITE',totag=True),src=B,dst=A,call_id=cid)
 udp(t+.03,'sip-'+call,5060,5060,msg('SIP/2.0 200 OK','INVITE',answer,True),src=B,dst=A,call_id=cid)
 udp(t+.04,'sip-'+call,5060,5060,msg('ACK sip:b@example.invalid SIP/2.0','ACK',totag=True),call_id=cid)
sip(1.5,'a',40000);sip(1.55,'b',41000)
for i in range(20):
 h=struct.pack('!BBHII',0x80,0,i+100,16000+i*160,0x11111111)
 udp(1.6+i*.02,'rtp-a-out',40000,40002,h+b'\xff'*160,ssrc=0x11111111,rtp_seq=i+100,rtp_ts=16000+i*160)
for i in range(20):
 h=struct.pack('!BBHII',0x80,0,i+300,48000+i*160,0x22222222);arrival=1.61+i*.02+(.035 if i==8 else 0)
 udp(arrival,'rtp-a-in',40002,40000,h+b'\xff'*160,src=B,dst=A,ssrc=0x22222222,rtp_seq=i+300,rtp_ts=48000+i*160)
 if i==12:udp(arrival+.002,'rtp-a-in',40002,40000,h+b'\xff'*160,src=B,dst=A,ssrc=0x22222222,rtp_seq=i+300,rtp_ts=48000+i*160)
for i in range(8):
 h=struct.pack('!BBHII',0x80,0,i+700,96000+i*160,0x33333333)
 udp(1.65+i*.02,'rtp-b-out',41000,41002,h+b'\xff'*160,ssrc=0x33333333,rtp_seq=i+700,rtp_ts=96000+i*160)
records.sort(key=lambda r:r['t'])
def omit(r):return (r['scenario']=='capture-omission' and r.get('tcp_len',0)>0 and r['src']==B) or (r['scenario']=='rtp-a-in' and r.get('rtp_seq')==305)
def write(name,rr,truncated=False):
 with open(name,'wb') as f:
  f.write(struct.pack('<IHHIIII',0xa1b2c3d4,2,4,0,0,65535,1))
  for r in rr:
   u=round(r['t']*1000000);data=r['data'];cap=data[:64] if truncated else data
   f.write(struct.pack('<IIII',BASE+u//1000000,u%1000000,len(cap),len(data)));f.write(cap)
 return hashlib.sha256(Path(name).read_bytes()).hexdigest()
manifest={'schema':1,'clock_base_epoch':BASE,'fictional':True,'linktype':1,'no_network':True,'files':{},'frames':[]}
for name,rr,trunc in [('wireshark-maintenance-mixed.pcap',[r for r in records if not omit(r)],False),('wireshark-maintenance-receiver.pcap',[r for r in records if not(r['scenario']=='retransmission' and r['t']==.530)],False),('wireshark-maintenance-truncated.pcap',[r for r in records if not omit(r)],True)]:
 manifest['files'][name]={'frames':len(rr),'sha256':write(name,rr,trunc),'snaplen_effective':64 if trunc else 65535,'observation':'synthetic-A' if 'receiver' not in name else 'synthetic-B/reference; RTP complete'}
 for n,r in enumerate(rr,1):manifest['frames'].append(dict(file=name,frame=n,time_relative=r['t']-rr[0]['t'],wire_time=r['t'],captured_len=min(64,len(r['data'])) if trunc else len(r['data']),original_len=len(r['data']),**{k:v for k,v in r.items() if k not in ['data','t']}))
Path('wireshark-maintenance-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
rows=[['host','clock','time_seconds','request','stage','detail'],['client','relative',.118,'q17','SEND','13 bytes'],['server','relative',.121,'q17','PARSE','GET'],['server','relative',.151,'q17','COMPLETE','READY'],['client','relative',.166,'q17','DISPLAY','READY'],['client','relative',.418,'q18','SEND','13 bytes'],['server','relative',.421,'q18','PARSE','GET'],['server','relative',.425,'q18','QUEUE','depth=8'],['client','relative',.918,'q18','TIMEOUT','deadline=500ms'],['server','relative',1.105,'q18','COMPLETE','READY; reply failed closed session']]
with open('wireshark-maintenance-logs.csv','w',newline='') as f:csv.writer(f).writerows(rows)
print(json.dumps(manifest['files'],indent=2))
