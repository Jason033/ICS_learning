using System.Globalization;
CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
// Original numerical teaching model. No RF generation, microphone, device or network.
string mode=args.FirstOrDefault()??"path";bool fault=args.Contains("--fault");
void Check(bool x,string label){if(!x)throw new InvalidOperationException(label);Console.WriteLine("PASS "+label);}
double DbmToMilliwatt(double d)=>Math.Pow(10,d/10);
double WattToDbm(double w)=>10*Math.Log10(w*1000);
if(mode=="all"){
    foreach(string name in new[]{"path","parameters","modulation","budget","quality","duplex","measurements","diagnose"}){
        Console.WriteLine("MODE "+name);Run(name,false);
    }
    Console.WriteLine("MODE path --fault");Run("path",true);
    Console.WriteLine("MODE diagnose --fault");Run("diagnose",true);
}else Run(mode,fault);
void Run(string mode,bool fault){
switch(mode){
case "path":{
    double[] audio=Enumerable.Range(0,800).Select(i=>fault?0:0.5*Math.Sin(2*Math.PI*1000*i/8000)).ToArray();
    double rms=Math.Sqrt(audio.Select(x=>x*x).Average());
    Console.WriteLine($"control=TX-reported audioSamples={audio.Length} audioRms={rms:F6} durationMs={audio.Length*1000.0/8000}");
    Console.WriteLine("RF-carrier-power observation does not prove modulation or remote audio.");
    Check(Math.Abs(rms-(fault?0:Math.Sqrt(0.125)))<1e-9,"known audio RMS");break;
}
case "parameters":{
    double f=150e6,c=299792458;
    double messageMax=3000,deviation=5000;
    Console.WriteLine($"fMHz={f/1e6} periodNs={1e9/f:F6} wavelengthM={c/f:F6} error2ppmHz={f*2e-6:F3}");
    Console.WriteLine($"AM message max={messageMax}Hz -> DSB ideal bandwidth={2*messageMax}Hz");
    Console.WriteLine($"FM deviation={deviation}Hz message max={messageMax}Hz -> Carson estimate={2*(deviation+messageMax)}Hz");
    Check(f>0&&messageMax>0&&deviation>=0,"frequency domain inputs");
    break;
}
case "modulation":{
    double symbols=4800;int constellation=16;double codeRate=0.75,payloadFraction=0.9,alpha=0.25;
    Check(constellation>1&&(constellation&(constellation-1))==0,"power-of-two constellation model");
    Check(codeRate>0&&codeRate<=1&&payloadFraction>0&&payloadFraction<=1&&alpha>=0&&alpha<=1,"rate and rolloff ranges");
    int bits=(int)Math.Log2(constellation);
    Console.WriteLine($"M={constellation} bitsPerSymbol={bits} symbolsPerSecond={symbols} rawBps={symbols*bits} informationBps={symbols*bits*codeRate} netPayloadBps={symbols*bits*codeRate*payloadFraction} rfSpanHz={symbols*(1+alpha)}");
    double max=1.8,min=.2;Console.WriteLine($"AM depth={(max-min)/(max+min):F3}");
    double[] points={-3,-1,1,3};Console.WriteLine("16QAM I/Q levels="+string.Join(',',points));
    Check(bits==4,"16 symbol states encode 4 bits");break;
}
case "budget":{
    double tx=30,txLoss=2,txGain=3,pathLoss=100,rxGain=3,rxLoss=1;
    double rx=tx-txLoss+txGain-pathLoss+rxGain-rxLoss;
    Console.WriteLine($"txDbm={tx} txW={DbmToMilliwatt(tx)/1000:F3} EIRPdbm={tx-txLoss+txGain:F3} rxDbm={rx:F3}");
    foreach(double vswr in new[]{1.5,2,3}){double rho=(vswr-1)/(vswr+1);Console.WriteLine($"VSWR={vswr} rho={rho:F6} reflectedPercent={rho*rho*100:F6} returnLossDb={-20*Math.Log10(rho):F6}");}
    Check(rx==-67,"link budget signs");Check(Math.Abs(WattToDbm(5)-36.9897000434)<1e-8,"5W dBm");break;
}
case "quality":{
    double bandwidth=20000,nf=6;double noise=-174+10*Math.Log10(bandwidth)+nf;
    double wanted=-100,snr=wanted-noise;
    Console.WriteLine($"noiseDbm={noise:F6} wantedDbm={wanted:F3} snrDb={snr:F6}");
    int wrongBits=25,totalBits=100000,badPackets=20,expectedPackets=1000;
    double ber=(double)wrongBits/totalBits,per=(double)badPackets/expectedPackets;
    Console.WriteLine($"bitErrors={wrongBits} totalBits={totalBits} BER={ber:F6}; badPackets={badPackets} packets={expectedPackets} PER={per:F6}");
    double bitError=.001;int packetBits=1000;Console.WriteLine($"independent-bit PER={1-Math.Pow(1-bitError,packetBits):F6}");
    Check(Math.Abs(noise-(-124.98970004336))<1e-8,"noise model units");
    Check(Math.Abs(ber-.00025)<1e-12&&Math.Abs(per-.02)<1e-12,"separate error denominators");
    break;
}
case "duplex":{
    int ready=50,audioStart=10,audioEnd=210;
    int early=Math.Max(0,Math.Min(audioEnd,ready)-audioStart),effective=Math.Max(0,audioEnd-Math.Max(audioStart,ready));
    Console.WriteLine($"PTT=0 TX-ready={ready} audio-start={audioStart} audio-end={audioEnd} earlyAudioMs={early} effectiveAudioMs={effective}");
    Check(early+effective==audioEnd-audioStart,"no-buffer interval partition");
    Console.WriteLine("REPEATER receive=U transmit=D; mobile transmit=U receive=D; labels are symbolic");
    Console.WriteLine("TX-RX isolation reduces local transmitter leakage; duplex alone is not a quality guarantee.");break;
}
case "measurements":{
    double reading=-45,attenuation=30,cableLoss=2;
    Console.WriteLine($"meterDbm={reading} externalAttenuatorDb={attenuation} cableDb={cableLoss} referencePlaneDbm={reading+attenuation+cableLoss:F3}");
    Console.WriteLine($"whiteNoise RBW 1kHz->10kHz expectedDeltaDb={10*Math.Log10(10):F3}");
    Console.WriteLine("Change one setting at a time; input protection and device limits are outside this calculation.");break;
}
case "diagnose":{
    Console.WriteLine(fault?"control TX-reported; audioRMS=0; RF-power-present; remote-RSSI=-70; remote-audio=silence":"control TX-reported; audioRMS=0.354; modulation-checked; remote recovered known tone");
    Console.WriteLine("Check independent control, baseband, RF, demodulation and playback boundaries.");break;
}
default:throw new ArgumentException(mode);
}
}
