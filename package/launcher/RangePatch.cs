using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace NativeRange {
 public sealed class Span { public uint Start; public uint End; public Span(uint a,uint b){Start=a;End=b;} }
 public sealed class Fingerprint { public uint Address; public int Length; public string Sha256; public Fingerprint(uint a,int n,string h){Address=a;Length=n;Sha256=h;} }
 public sealed class Plan {
  public int ProcessId; public string Executable; public string Sha256; public int ImageSize;
  public uint TrackSite,CourseSite,CourseOriginal,TrackVtable,CourseVtable;
  public bool Fixture; public bool FixtureFailureAfterFirstWrite; public List<Span> BusySpans=new List<Span>(); public List<Fingerprint> Fingerprints=new List<Fingerprint>();
 }
 public sealed class ThreadRecord { public uint Id; public uint PriorSuspendCount; public uint Eip; public IntPtr Handle; }
 public sealed class Report {
  public bool Installed; public bool Fixture; public int ProcessId; public string Executable; public string ExecutableSha256;
  public string Error; public long CodeAddress; public long DataAddress; public int CodeBytes;
  public int ReadCalls,ReadBytes,WriteCalls,WriteBytes; public double SuspendedMilliseconds;
  public string ProcessAccess="0x0438 QUERY_INFORMATION|VM_OPERATION|VM_READ|VM_WRITE";
  public bool RolledBack; public bool ProtectionRestored=true; public bool AllThreadsResumed=true;
  public uint TrackSite,CourseSite; public string TrackBefore,TrackAfter,CourseBefore,CourseAfter;
  public List<string> Messages=new List<string>(); public List<ThreadRecord> Threads=new List<ThreadRecord>();
  public string Lifetime="On success code/counter allocations remain valid until this target process exits; no client files modified";
 }
 public sealed class Counters { public uint TrackTotal,TrackAccepted,CourseTotal,CourseAccepted; public bool Valid; }
 public static class Patcher {
  const uint MEM_COMMIT=0x1000,MEM_RESERVE=0x2000,MEM_RELEASE=0x8000,PAGE_RW=4,PAGE_RX=0x20,PAGE_RWX=0x40;
  const string GameHash="629f084e2a12c6fa1ff0ea603b90f8768454d13a1bc2df6a8504f8aa06fd6194";
  static readonly byte[] TrackOriginal=Hex("558bec81ec34010000");
  [StructLayout(LayoutKind.Sequential)] struct TE { public uint Size,Usage,Id,Owner; public int BasePriority,DeltaPriority; public uint Flags; }
  [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint a,bool inherit,int pid);
  [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr a,byte[] b,UIntPtr n,out UIntPtr read);
  [DllImport("kernel32.dll",SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr a,byte[] b,UIntPtr n,out UIntPtr wrote);
  [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr h,IntPtr a,UIntPtr n,uint type,uint protect);
  [DllImport("kernel32.dll",SetLastError=true)] static extern bool VirtualFreeEx(IntPtr h,IntPtr a,UIntPtr n,uint type);
  [DllImport("kernel32.dll",SetLastError=true)] static extern bool VirtualProtectEx(IntPtr h,IntPtr a,UIntPtr n,uint p,out uint old);
  [DllImport("kernel32.dll",SetLastError=true)] static extern bool FlushInstructionCache(IntPtr h,IntPtr a,UIntPtr n);
  [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr CreateToolhelp32Snapshot(uint flags,uint pid);
  [DllImport("kernel32.dll",SetLastError=true)] static extern bool Thread32First(IntPtr h,ref TE e);
  [DllImport("kernel32.dll",SetLastError=true)] static extern bool Thread32Next(IntPtr h,ref TE e);
  [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenThread(uint a,bool inherit,uint tid);
  [DllImport("kernel32.dll",SetLastError=true)] static extern uint SuspendThread(IntPtr h);
  [DllImport("kernel32.dll",SetLastError=true)] static extern uint ResumeThread(IntPtr h);
  [DllImport("kernel32.dll",SetLastError=true)] static extern bool GetThreadContext(IntPtr h,IntPtr context);
  [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
  static IntPtr Ptr(uint a){return new IntPtr(unchecked((int)a));}
  static uint Addr(IntPtr p){return unchecked((uint)p.ToInt32());}
  static Exception Fail(string s){return new InvalidOperationException(s+" (Win32="+Marshal.GetLastWin32Error()+")");}
  public static byte[] Hex(string s){byte[] b=new byte[s.Length/2];for(int i=0;i<b.Length;i++)b[i]=Convert.ToByte(s.Substring(i*2,2),16);return b;}
  static string ToHex(byte[] b){return BitConverter.ToString(b).Replace("-","").ToLowerInvariant();}
  static string Hash(byte[] b){using(SHA256 h=SHA256.Create())return ToHex(h.ComputeHash(b));}
  static string FileHash(string p){using(FileStream f=File.OpenRead(p))using(SHA256 h=SHA256.Create())return ToHex(h.ComputeHash(f));}
  static void Require(bool v,string s){if(!v)throw new InvalidOperationException(s);}
  static byte[] Read(IntPtr h,uint a,int n,Report r){
   Require(n>0&&r.ReadCalls<64&&r.ReadBytes+n<=32768,"Remote read budget exceeded");byte[] b=new byte[n];UIntPtr got;
   r.ReadCalls++; if(!ReadProcessMemory(h,Ptr(a),b,(UIntPtr)(uint)n,out got)||got.ToUInt64()!=(ulong)n)throw Fail("Read failed"); r.ReadBytes+=n;return b;
  }
  static void Write(IntPtr h,uint a,byte[] b,Report r){
   Require(r.WriteCalls<32&&r.WriteBytes+b.Length<=8192,"Remote write budget exceeded");UIntPtr done;r.WriteCalls++;
   if(!WriteProcessMemory(h,Ptr(a),b,(UIntPtr)(uint)b.Length,out done)||done.ToUInt64()!=(ulong)b.Length)throw Fail("Write failed");r.WriteBytes+=b.Length;
  }
  static void Flush(IntPtr h,uint a,int n){if(!FlushInstructionCache(h,Ptr(a),(UIntPtr)(uint)n))throw Fail("Instruction cache flush failed");}
  static byte[] Rel(byte opcode,uint site,uint target,int n){
   long d=(long)target-site-5;Require(d>=int.MinValue&&d<=int.MaxValue,"rel32 out of range");byte[] b=Enumerable.Repeat((byte)0x90,n).ToArray();b[0]=opcode;Buffer.BlockCopy(BitConverter.GetBytes((int)d),0,b,1,4);return b;
  }
  static void U32(List<byte> b,uint v){b.AddRange(BitConverter.GetBytes(v));}
  static void Inc(List<byte>b,uint a){b.AddRange(new byte[]{0xf0,0xff,0x05});U32(b,a);}
  static void Set(List<byte>b,int offset){b.AddRange(new byte[]{0xc7,0x81});U32(b,unchecked((uint)offset));U32(b,0x463b8000);}
  static void Tail(List<byte>b,uint baseAddress,uint target){b.AddRange(Rel(0xe9,baseAddress+(uint)b.Count,target,5));}
  public static byte[] TrackThunk(uint address,uint data,uint vtable,uint resume){
   List<byte>b=new List<byte>();b.Add(0x9c);Inc(b,data+16);b.AddRange(new byte[]{0x81,0x39});U32(b,vtable);b.AddRange(new byte[]{0x0f,0x85});int branch=b.Count;U32(b,0);
   Set(b,0x2c);Set(b,0x30);Set(b,0x280);Set(b,0x284);Inc(b,data+20);int skip=b.Count;
   byte[] displacement=BitConverter.GetBytes(skip-(branch+4));for(int i=0;i<4;i++)b[branch+i]=displacement[i];
   b.Add(0x9d);b.AddRange(TrackOriginal);Tail(b,address,resume);return b.ToArray();
  }
  public static byte[] CourseThunk(uint address,uint data,uint vtable,uint original){
   List<byte>b=new List<byte>();b.Add(0x9c);Inc(b,data+24);b.AddRange(new byte[]{0x81,0xb9});U32(b,unchecked((uint)-0x110));U32(b,vtable);b.AddRange(new byte[]{0x0f,0x85});int branch=b.Count;U32(b,0);
   Set(b,8);Set(b,12);Inc(b,data+28);int skip=b.Count;byte[] displacement=BitConverter.GetBytes(skip-(branch+4));for(int i=0;i<4;i++)b[branch+i]=displacement[i];
   b.Add(0x9d);Tail(b,address,original);return b.ToArray();
  }
  static List<uint> ThreadIds(int pid){
   List<uint> ids=new List<uint>();IntPtr snap=CreateToolhelp32Snapshot(4,0);if(snap==new IntPtr(-1))throw Fail("Thread snapshot failed");
   try{TE e=new TE();e.Size=(uint)Marshal.SizeOf(typeof(TE));if(!Thread32First(snap,ref e))throw Fail("Thread enumeration failed");do{if(e.Owner==(uint)pid)ids.Add(e.Id);e.Size=(uint)Marshal.SizeOf(typeof(TE));}while(Thread32Next(snap,ref e));}finally{CloseHandle(snap);}Require(ids.Count>0&&ids.Count<=128,"Thread count outside budget");return ids;
  }
  static void CheckTime(Stopwatch timer,int limit){Require(timer.ElapsedMilliseconds<=limit,"Thread suspension deadline reached");}
  static void Freeze(Plan p,Report r,Stopwatch timer){
   HashSet<uint> known=new HashSet<uint>();
   for(int pass=0;pass<3;pass++){
    List<uint> ids=ThreadIds(p.ProcessId);bool added=false;
    foreach(uint id in ids){if(known.Contains(id))continue;CheckTime(timer,400);Require(r.Threads.Count<128,"Thread budget reached");
     IntPtr h=OpenThread(0x4a,false,id);if(h==IntPtr.Zero)throw Fail("OpenThread failed");uint previous=SuspendThread(h);if(previous==uint.MaxValue){CloseHandle(h);throw Fail("SuspendThread failed");}
     ThreadRecord record=new ThreadRecord();record.Id=id;record.PriorSuspendCount=previous;record.Handle=h;r.Threads.Add(record);known.Add(id);added=true;
     IntPtr context=Marshal.AllocHGlobal(736);try{for(int k=0;k<736;k++)Marshal.WriteByte(context,k,0);Marshal.WriteInt32(context,0,0x10001);if(!GetThreadContext(h,context))throw Fail("GetThreadContext failed");record.Eip=unchecked((uint)Marshal.ReadInt32(context,184));}finally{Marshal.FreeHGlobal(context);}
     foreach(Span span in p.BusySpans)Require(record.Eip<span.Start||record.Eip>=span.End,"A target thread is in a patch/loading region; retry from idle lobby");
     Require(record.Eip<p.TrackSite||record.Eip>=p.TrackSite+9,"Thread in Track hook window");Require(record.Eip<p.CourseSite||record.Eip>=p.CourseSite+5,"Thread in Course hook window");
    }
    if(!added){CheckTime(timer,400);return;}
   }
   throw new InvalidOperationException("Thread set did not stabilize in three passes");
  }
  static void Resume(Report r){for(int i=r.Threads.Count-1;i>=0;i--){ThreadRecord t=r.Threads[i];if(ResumeThread(t.Handle)==uint.MaxValue){r.AllThreadsResumed=false;r.Messages.Add("ResumeThread failed for "+t.Id);}CloseHandle(t.Handle);t.Handle=IntPtr.Zero;}}
  static uint Verify(Plan p){
   Require(IntPtr.Size==4,"Run this helper with 32-bit Windows PowerShell");Require(p.ProcessId!=Process.GetCurrentProcess().Id,"Self patch forbidden");
   string expected=Path.GetFullPath(p.Executable);Require(FileHash(expected)==p.Sha256.ToLowerInvariant(),"Executable SHA256 mismatch");
   Process process=Process.GetProcessById(p.ProcessId);ProcessModule module=process.MainModule;
   Require(String.Equals(Path.GetFullPath(module.FileName),expected,StringComparison.OrdinalIgnoreCase),"Executable path mismatch");Require(module.ModuleMemorySize==p.ImageSize,"Module size mismatch");
   return Addr(module.BaseAddress);
  }
  public static Report ApplyGame(int pid,string executable){
   string path=Path.GetFullPath(executable);Require(path.IndexOf("\\runtime\\nord-full-v10\\KartRider_5136\\KartRider.exe",StringComparison.OrdinalIgnoreCase)>=0,"Only the isolated nord-full-v10 client may be patched");
   Plan p=new Plan();p.ProcessId=pid;p.Executable=path;p.Sha256=GameHash;p.ImageSize=0x1418000;uint b=Verify(p);Require(b==0x400000,"Unexpected image base");
   p.TrackSite=b+0x56ab80;p.CourseSite=b+0x55dfcd;p.CourseOriginal=b+0xb2b9b0;p.TrackVtable=b+0xc87de0;p.CourseVtable=b+0xc87b48;
   p.BusySpans.Add(new Span(b+0x56ab80,b+0x56b35a));p.BusySpans.Add(new Span(b+0x55d270,b+0x55edb8));p.BusySpans.Add(new Span(b+0xb2b9b0,b+0xb2c278));
   p.Fingerprints.Add(new Fingerprint(b+0x56ab80,0x7da,"f8338bed4311164f64961dd28b454d8cd472f9341bd171ef885c4e2bd488301d"));
   p.Fingerprints.Add(new Fingerprint(b+0x55d270,0x1b48,"8d64fc89ff64bdeeea42a3d3f69cbc1bfda84f2f60cb7a7925bb9eaf419ce1d1"));
   p.Fingerprints.Add(new Fingerprint(b+0x568800,0xce,"24cc5a8785b9c2108bc430b1bf638a8f16a549e7f3ed74767168ba502953758c"));
   p.Fingerprints.Add(new Fingerprint(b+0xb2c630,0x338,"d115074802e83af89617549df62623619156ccfa85c2d53c88bfe9278e9fdfcb"));
   return Apply(p);
  }
  public static Report ApplyFixture(Plan p){
   Require(p.Fixture&&Path.GetFileName(p.Executable)=="RangePatchOwnedFixture.exe"&&Path.GetFullPath(p.Executable).IndexOf("\\evidence\\native-range-research\\owned-patch-fixture\\",StringComparison.OrdinalIgnoreCase)>=0,"Fixture plan path is not owned fixture");return Apply(p);
  }
  static Report Apply(Plan p){
   Report r=new Report();r.ProcessId=p.ProcessId;r.Executable=p.Executable;r.ExecutableSha256=p.Sha256;r.Fixture=p.Fixture;r.TrackSite=p.TrackSite;r.CourseSite=p.CourseSite;
   IntPtr h=IntPtr.Zero,code=IntPtr.Zero,data=IntPtr.Zero;bool success=false,safeToFree=true;byte[] oldTrack=null,oldCourse=null;uint oldTrackProtection=0,oldCourseProtection=0;bool trackProtected=false,courseProtected=false,trackAttempt=false,courseAttempt=false;Stopwatch suspended=null;
   try{
    Verify(p);h=OpenProcess(0x438,false,p.ProcessId);if(h==IntPtr.Zero)throw Fail("OpenProcess failed");
    foreach(Fingerprint f in p.Fingerprints)Require(Hash(Read(h,f.Address,f.Length,r))==f.Sha256,"Captured-code fingerprint mismatch at "+f.Address.ToString("x"));
    oldTrack=Read(h,p.TrackSite,9,r);oldCourse=Read(h,p.CourseSite,5,r);Require(oldTrack.SequenceEqual(TrackOriginal),"Track prologue mismatch");Require(oldCourse.SequenceEqual(Rel(0xe8,p.CourseSite,p.CourseOriginal,5)),"Course call mismatch");
    r.TrackBefore=ToHex(oldTrack);r.CourseBefore=ToHex(oldCourse);
    code=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)4096,MEM_COMMIT|MEM_RESERVE,PAGE_RW);data=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)4096,MEM_COMMIT|MEM_RESERVE,PAGE_RW);if(code==IntPtr.Zero||data==IntPtr.Zero)throw Fail("Private allocation failed");
    uint ca=Addr(code),da=Addr(data);Require(ca<0x7fff0000&&da<0x7fff0000,"Allocation outside admitted x86 low-address region");r.CodeAddress=ca;r.DataAddress=da;
    byte[] track=TrackThunk(ca,da,p.TrackVtable,p.TrackSite+9),course=CourseThunk(ca+256,da,p.CourseVtable,p.CourseOriginal);
    byte[] payload=new byte[256+course.Length];Buffer.BlockCopy(track,0,payload,0,track.Length);Buffer.BlockCopy(course,0,payload,256,course.Length);r.CodeBytes=payload.Length;
    byte[] header=new byte[32];Buffer.BlockCopy(Encoding.ASCII.GetBytes("NRG1"),0,header,0,4);Buffer.BlockCopy(BitConverter.GetBytes(p.ProcessId),0,header,4,4);
    Write(h,ca,payload,r);Write(h,da,header,r);Require(Read(h,ca,payload.Length,r).SequenceEqual(payload),"Private code readback mismatch");
    uint previous;if(!VirtualProtectEx(h,code,(UIntPtr)4096,PAGE_RX,out previous))throw Fail("Code RX protection failed");Flush(h,ca,payload.Length);
    byte[] trackPatch=Rel(0xe9,p.TrackSite,ca,9),coursePatch=Rel(0xe8,p.CourseSite,ca+256,5);
    suspended=Stopwatch.StartNew();Freeze(p,r,suspended);
    Require(Read(h,p.TrackSite,9,r).SequenceEqual(oldTrack)&&Read(h,p.CourseSite,5,r).SequenceEqual(oldCourse),"Hook bytes changed before transaction");CheckTime(suspended,400);
    if(!VirtualProtectEx(h,Ptr(p.TrackSite),(UIntPtr)9,PAGE_RWX,out oldTrackProtection))throw Fail("Track protect failed");trackProtected=true;
    if(!VirtualProtectEx(h,Ptr(p.CourseSite),(UIntPtr)5,PAGE_RWX,out oldCourseProtection))throw Fail("Course protect failed");courseProtected=true;
    CheckTime(suspended,400);trackAttempt=true;Write(h,p.TrackSite,trackPatch,r);
    if(p.Fixture&&p.FixtureFailureAfterFirstWrite)throw new InvalidOperationException("Owned-fixture injected failure after the first hook write");
    courseAttempt=true;Write(h,p.CourseSite,coursePatch,r);Flush(h,p.TrackSite,9);Flush(h,p.CourseSite,5);
    Require(Read(h,p.TrackSite,9,r).SequenceEqual(trackPatch)&&Read(h,p.CourseSite,5,r).SequenceEqual(coursePatch),"Hook readback mismatch");CheckTime(suspended,500);
    r.TrackAfter=ToHex(trackPatch);r.CourseAfter=ToHex(coursePatch);success=true;r.Installed=true;
   }catch(Exception ex){r.Error=ex.Message;
    if(h!=IntPtr.Zero&&(trackAttempt||courseAttempt)){
     try{if(trackAttempt){Write(h,p.TrackSite,oldTrack,r);Flush(h,p.TrackSite,9);Require(Read(h,p.TrackSite,9,r).SequenceEqual(oldTrack),"Track rollback mismatch");}if(courseAttempt){Write(h,p.CourseSite,oldCourse,r);Flush(h,p.CourseSite,5);Require(Read(h,p.CourseSite,5,r).SequenceEqual(oldCourse),"Course rollback mismatch");}r.RolledBack=true;}catch(Exception rollback){safeToFree=false;r.Messages.Add("CRITICAL rollback failure; remote allocations retained until target exit: "+rollback.Message);}
    }
   }finally{
    uint ignored;
    if(courseProtected&&!VirtualProtectEx(h,Ptr(p.CourseSite),(UIntPtr)5,oldCourseProtection,out ignored)){r.ProtectionRestored=false;r.Messages.Add("Course protection restore failed");}
    if(trackProtected&&!VirtualProtectEx(h,Ptr(p.TrackSite),(UIntPtr)9,oldTrackProtection,out ignored)){r.ProtectionRestored=false;r.Messages.Add("Track protection restore failed");}
    if(suspended!=null){Resume(r);suspended.Stop();r.SuspendedMilliseconds=suspended.Elapsed.TotalMilliseconds;}
    if(!success&&safeToFree&&h!=IntPtr.Zero){if(code!=IntPtr.Zero)VirtualFreeEx(h,code,UIntPtr.Zero,MEM_RELEASE);if(data!=IntPtr.Zero)VirtualFreeEx(h,data,UIntPtr.Zero,MEM_RELEASE);}
    if(h!=IntPtr.Zero)CloseHandle(h);
   }
   return r;
  }
  public static Counters ReadCounters(Report r){
   Require(r.Installed,"No installed patch report");Process process=Process.GetProcessById(r.ProcessId);Require(String.Equals(process.MainModule.FileName,r.Executable,StringComparison.OrdinalIgnoreCase),"Counter target identity changed");
   IntPtr h=OpenProcess(0x410,false,r.ProcessId);if(h==IntPtr.Zero)throw Fail("Counter query failed");try{Report budget=new Report();byte[] b=Read(h,(uint)r.DataAddress,32,budget);Require(Encoding.ASCII.GetString(b,0,4)=="NRG1"&&BitConverter.ToInt32(b,4)==r.ProcessId,"Counter block identity mismatch");return new Counters{Valid=true,TrackTotal=BitConverter.ToUInt32(b,16),TrackAccepted=BitConverter.ToUInt32(b,20),CourseTotal=BitConverter.ToUInt32(b,24),CourseAccepted=BitConverter.ToUInt32(b,28)};}finally{CloseHandle(h);}
  }
 }
}
