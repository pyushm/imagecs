using System;
using System.Text;
using System.IO;

namespace NNTP
{
	enum Method
	{
		uuEncode,
		yEnc,
		MIME,
		none
	}
	class MessageDecoder 
	{
		DecodeMethod decodeMethod;
		string encodedData;
		int encodedDataLineCount=0;
		StringWriter errors;
		public int PartNumber				{ get { return decodeMethod.PartNumber; } }
		public string Errors				{ get { return errors.ToString(); } }
		public string DataName				{ get { return decodeMethod.DataName; } }
		public MessageDecoder()				{ decodeMethod=new DecodeMethod(); }
		public byte[] GetDecodedData(string article, int partNumber)
		{
			errors=new StringWriter();
			int partNum=partNumber;
			if(SetEncodedText(article, partNum))
				return GetDecoadedBytes();
			return new byte[0];
		}
		bool SetEncodedText(string article, int partNum)
		{
			encodedDataLineCount=0;
			TextReader r=new StringReader(article);
			string l;
			StringWriter sb=new StringWriter();
			while((l=r.ReadLine())!=null)
			{
				if(l.Length==0)
					continue;
				if(decodeMethod.Undefined && UuEncode.MethodFound(l, partNum))
					decodeMethod=new UuEncode(errors, partNum);
				if(decodeMethod.Undefined && YEnc.MethodFound(l, partNum))	
					decodeMethod=new YEnc(errors, partNum);
				if(decodeMethod.Undefined && MIME.MethodFound(l, partNum))	
					decodeMethod=new MIME(errors, partNum);
				if(!decodeMethod.Undefined)
				{
					sb.WriteLine(l);
					encodedDataLineCount++;
					if(decodeMethod.FoundFooter(l))
						break;
				}
			}
			encodedData=sb.ToString();
			decodeMethod.NumLines=encodedDataLineCount;
			return encodedDataLineCount>0;
		}
		byte[] GetDecoadedBytes()			
		{
			if(encodedData==null || encodedData.Length==0 || encodedDataLineCount==0)
				return new byte[0];
			byte[][] binaryData=(byte[][])Array.CreateInstance(typeof(byte[]), encodedDataLineCount);
			string l;
			int numBytes=0;
			int lineNumber=0;
			try
			{
				StringReader sr=new StringReader(encodedData);
				while((l=sr.ReadLine())!=null)
				{
					byte[] ba=decodeMethod.DecodeDataLine(l, lineNumber);
					if(ba!=null)
						numBytes+=ba.Length;
					else if(numBytes>0)
						break;		// first invalid line interrupts data flow
					binaryData[lineNumber]=ba;
					lineNumber++;
				}
			}
			catch(Exception ex)
			{
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                errors.WriteLine(ex.Message);
			}
			decodeMethod.Validate(numBytes);
			byte[] binaryArray=new byte[numBytes];
			int offset=0;
			for(int i=0; i<lineNumber; i++)
			{
				byte[] ba=binaryData[i];
				if(ba!=null)
				{
					for(int j=0; j<binaryData[i].Length; j++)
						binaryArray[j+offset]=binaryData[i][j];
					offset+=binaryData[i].Length;
				}
			}
			return binaryArray;
		}

	}
	class DecodeMethod
	{
		Method method;
		protected string dataName="";		// name of encoded data file
		protected int numBytesInLine=0;		// number of bytes in an encoded line
		protected int partNumber=0;			// part of multi-part article (=0 if single message)
		protected int dataSize=0;			// total data size
		protected int partBegin=0;			// bart data start index
		protected int partEnd=0;			// bart data end index
		protected StringWriter errorMessage;
		public int NumLines;

		public DecodeMethod()
		{ 
			method=Method.none;
		}
		protected DecodeMethod(Method m, int partNum, StringWriter err)
		{ 
			method=m;
			partNumber=partNum;
			errorMessage=err;
		}
		public bool Undefined				{ get { return method==Method.none; } }
		public string DataName				{ get { return dataName; } }
		public int PartNumber				{ get { return partNumber; } }
		public virtual byte[] DecodeDataLine(string l, int lineNum) { return null; }
		public virtual bool FoundFooter(string l) { return false; }
		public virtual void Validate(int numBytes) { }
		protected int GetIntField(string l, string field)
		{
			try
			{
				int indb=l.IndexOf(field)+field.Length;
				if(indb<field.Length)
					return 0;
				int inde=l.IndexOf(' ', indb);
				if(inde<0)
					inde=l.Length;
				return int.Parse(l.Substring(indb, inde-indb));
			}
			catch
			{
				return 0;
			}
		}
		protected string GetStringField(string l, string field)
		{
			int len=field.Length;
			int pos=field.IndexOf('*');
			if(pos>=0)
				field=field.Substring(0, pos);
			string val="";
			try
			{
				int indb=l.IndexOf(field);
				if(indb>=0)
					val=l.Substring(indb+len);
			}
			catch { }
			return val;
		}
	}
	class YEnc : DecodeMethod
	{
		//		string crcString;
		//		uint crc_val;
		//		uint[] crc_tab;
		public static bool MethodFound(string l, int partNumber)
		{	//=ybegin part=1 line=128 size=18819 name=tent 061.jpg
			//=ybegin line=128 size=18819 name=tent 061.jpg
			return l.StartsWith("=ybegin ");
		}
		public YEnc(StringWriter err, int partNum) : base(Method.yEnc, partNum, err)
		{
			//			crc_tab=new uint[]{
			//			0x00000000, 0x77073096, 0xee0e612c, 0x990951ba, 0x076dc419, 0x706af48f, 0xe963a535, 0x9e6495a3,
			//			0x0edb8832, 0x79dcb8a4, 0xe0d5e91e, 0x97d2d988, 0x09b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91,
			//			0x1db71064, 0x6ab020f2, 0xf3b97148, 0x84be41de, 0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7,
			//			0x136c9856, 0x646ba8c0, 0xfd62f97a, 0x8a65c9ec, 0x14015c4f, 0x63066cd9, 0xfa0f3d63, 0x8d080df5,
			//			0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172, 0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b,
			//			0x35b5a8fa, 0x42b2986c, 0xdbbbc9d6, 0xacbcf940, 0x32d86ce3, 0x45df5c75, 0xdcd60dcf, 0xabd13d59,
			//			0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116, 0x21b4f4b5, 0x56b3c423, 0xcfba9599, 0xb8bda50f,
			//			0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924, 0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d,
			//			0x76dc4190, 0x01db7106, 0x98d220bc, 0xefd5102a, 0x71b18589, 0x06b6b51f, 0x9fbfe4a5, 0xe8b8d433,
			//			0x7807c9a2, 0x0f00f934, 0x9609a88e, 0xe10e9818, 0x7f6a0dbb, 0x086d3d2d, 0x91646c97, 0xe6635c01,
			//			0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e, 0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457,
			//			0x65b0d9c6, 0x12b7e950, 0x8bbeb8ea, 0xfcb9887c, 0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65,
			//			0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2, 0x4adfa541, 0x3dd895d7, 0xa4d1c46d, 0xd3d6f4fb,
			//			0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0, 0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9,
			//			0x5005713c, 0x270241aa, 0xbe0b1010, 0xc90c2086, 0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,
			//			0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4, 0x59b33d17, 0x2eb40d81, 0xb7bd5c3b, 0xc0ba6cad,
			//			0xedb88320, 0x9abfb3b6, 0x03b6e20c, 0x74b1d29a, 0xead54739, 0x9dd277af, 0x04db2615, 0x73dc1683,
			//			0xe3630b12, 0x94643b84, 0x0d6d6a3e, 0x7a6a5aa8, 0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1,
			//			0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe, 0xf762575d, 0x806567cb, 0x196c3671, 0x6e6b06e7,
			//			0xfed41b76, 0x89d32be0, 0x10da7a5a, 0x67dd4acc, 0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5,
			//			0xd6d6a3e8, 0xa1d1937e, 0x38d8c2c4, 0x4fdff252, 0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b,
			//			0xd80d2bda, 0xaf0a1b4c, 0x36034af6, 0x41047a60, 0xdf60efc3, 0xa867df55, 0x316e8eef, 0x4669be79,
			//			0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236, 0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f,
			//			0xc5ba3bbe, 0xb2bd0b28, 0x2bb45a92, 0x5cb36a04, 0xc2d7ffa7, 0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d,
			//			0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x026d930a, 0x9c0906a9, 0xeb0e363f, 0x72076785, 0x05005713,
			//			0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38, 0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21,
			//			0x86d3d2d4, 0xf1d4e242, 0x68ddb3f8, 0x1fda836e, 0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,
			//			0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c, 0x8f659eff, 0xf862ae69, 0x616bffd3, 0x166ccf45,
			//			0xa00ae278, 0xd70dd2ee, 0x4e048354, 0x3903b3c2, 0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db,
			//			0xaed16a4a, 0xd9d65adc, 0x40df0b66, 0x37d83bf0, 0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,
			//			0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6, 0xbad03605, 0xcdd70693, 0x54de5729, 0x23d967bf,
			//			0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94, 0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d};
		}
		public override byte[] DecodeDataLine(string l, int lineNum)
		{
			if(lineNum==0 && ParseHeader(l))
				return null;
			if(lineNum==1 && ParsePartLine(l))
				return null;
			if(lineNum==NumLines-1 && FoundFooter(l))
				return null;
			byte[] ba=new byte[numBytesInLine];
			int byteCounter=0;
			bool esc=false;
			for(int i=0; i<l.Length; i++)
			{
				char c=l[i];
				if(c == '=')  // The escape character comes in
				{
					esc=true;
					continue;
				}
				if(esc)
				{
					if(c==0) 
						throw new Exception("Last char cannot be escape char: "+l);
					c=(char)(c-64);
					esc=false;
				}
				if(byteCounter>=numBytesInLine)
					throw new Exception("Byte array overflow in line: "+l);
				ba[byteCounter]=(byte)(c-42);  // Subtract the secret number
				byteCounter++;
				//				CrcAdd(c);
			}
			if(byteCounter==numBytesInLine)
				return ba;
			byte[] ba1=new byte[byteCounter];
			for(int i=0; i<byteCounter; i++)
				ba1[i]=ba[i];
			return ba1;
		}
		public override bool FoundFooter(string l)
		{	// multi-part end line: =yend size=18819 part=1 pcrc32=e8724d74
			// single file end line: =yend size=28836 crc32=36a9c491
			return l.StartsWith("=yend ");
		}
		public override void Validate(int numBytes)
		{
		//			uint crc=HexToUint(crcString.Trim());
		//			if(crc!=crc_val)	calculated not correct
		//				errorMessage.WriteLine("CRC differ: written="+crc+" calculated="+crc_val);
			int len=dataSize;
			if(partNumber>0)
				len=partEnd-partBegin+1;
			if(numBytes!=len)
				errorMessage.WriteLine("Number ob bytes differ: written="+len+" calculated="+numBytes);
		}
		bool ParseHeader(string l)		
		{	//=ybegin part=1 line=128 size=18819 name=tent 061.jpg
			if(l.StartsWith("=ybegin "))
			{
				dataName=GetStringField(l, "name=");
				if(dataName.Length==0)
				{
					errorMessage.WriteLine("yEnc header format error: file name not specified");
					dataName="UndefinedYEncFileName.jpg";
				}
				partNumber=GetIntField(l, "part=");
				numBytesInLine=GetIntField(l, "line=");
				dataSize=GetIntField(l, "size=");
				//			crc_val = 0xffffffffU;
				return true;
			}
			return false;
		}	
		bool ParsePartLine(string l)		
		{	//=ypart begin=1 end=18819
			if(l.StartsWith("=ypart"))
			{
				partBegin=GetIntField(l, "begin=");
				partEnd=GetIntField(l, "end=");
				return true;
			}
			return false;
		}	
//		bool ParseEndLine(string l)			
//		{	// multi-part end line: =yend size=18819 part=1 pcrc32=e8724d74
//			// single file end line: =yend size=28836 crc32=36a9c491
//			if(l.StartsWith("=yend"))
//			{
//				int size=GetIntField(l, "size=");
//				int part=GetIntField(l, "part=");
//				crcString=GetStringField(l, "crc32=");
//				return true;
//			}
//			return false;
//		}
//		void CrcAdd(uint c)					
//		{
//			uint ch1, ch2, cc;
//			cc= (c) & 0x000000ffU;
//			ch1=(crc_val ^ cc) & 0xffU;
//			ch1=crc_tab[ch1];
//			ch2=(crc_val>>8) & 0xffffffU;
//			crc_val=ch1 ^ ch2;
//		}
//		protected uint HexToUint(string text)
//		{
//			if(text==null) return 0xffffffffu;
//			uint res=0;
//			foreach(char c in text)
//			{
//				if ((c>='0')&(c<='9'))
//					res=(res<<4)+((uint)(c-48) & 0x0F);
//				else if ((c>='A')&(c<='F'))
//					res=(res<<4)+((uint)(c-55) & 0x0F);
//				else if ((c>='a')&(c<='f'))
//					res=(res<<4)+((uint)(c-87) & 0x0F);
//				else
//					throw new Exception("Unexpected character in hex string "+text);
//			}
//			return res;
//		}
	}
	class UuEncode : DecodeMethod
	{
		public static bool MethodFound(string l, int partNumber)
		{	// begin 644 test.jpg
			if(partNumber<2)
				return l.StartsWith("begin ");
			else
				return IsDataLine(l);
		}
		public UuEncode(StringWriter err, int partNum) : base(Method.uuEncode, partNum, err){ }
		public override byte[] DecodeDataLine(string l, int lineNum)
		{ 
			if(lineNum==0 && ParseHeader(l))
				return null;
			if(lineNum==NumLines-2 && l[0]=='`')
				return null;
			if(lineNum==NumLines-1 && FoundFooter(l))
				return null;
			numBytesInLine=(int)l[0]-32;
			byte[] ba=new byte[numBytesInLine];
			int numChars = l.Length-1;
			int numChanks=numChars/4;
			if(numBytesInLine>numChanks*3 && numBytesInLine<=numChanks*3+3)
			{
				numChanks++;
				switch(numChanks*4-numChars)
				{
					case 1:
						l+=' ';
						break;
					case 2:
						l+="  ";
						break;
					case 3:
						l+="   ";
						break;
				}
			}
			if(!IsDataLine(l))
				return null;
			byte b20=0x20;
			byte b3f=0x3f;
			for(int c=0; c<numChanks; c++) 
			{ 
				int i=4*c+1;
				byte byte1=(byte)((l[i]^b20) & b3f);
				byte byte2=(byte)((l[i+1]^b20) & b3f);
				byte byte3=(byte)((l[i+2]^b20) & b3f);
				byte byte4=(byte)((l[i+3]^b20) & b3f);
				ba[3*c]=(byte)(byte1<<2 | byte2>>4); 
				if(3*c+1<numBytesInLine)
					ba[3*c+1]=(byte)(byte2<<4 | byte3>>2); 
				if(3*c+2<numBytesInLine)
					ba[3*c+2]=(byte)(byte3<<6 | byte4); 
			} 
			return ba; 
		}
		public override bool FoundFooter(string l)
		{	// multi-part end line: =yend size=18819 part=1 pcrc32=e8724d74
			// single file end line: =yend size=28836 crc32=36a9c491
			return l.StartsWith("end");
		}
		bool ParseHeader(string l)			
		{	// begin 644 test.jpg
			if(l.StartsWith("begin "))	
			{
				dataName=GetStringField(l, "begin ****");
				if(dataName.Length==0)
				{
					errorMessage.WriteLine("UuEncoded header format error: file name not specified");
					dataName="UndefinedUuEncodeFileName.jpg";
				}
				return true;
			}
			return false;
		}	
		static bool IsDataLine(string l)			
		{
			return 4*(l[0]-32)==3*(l.Length-1); // uuEncode data line
		}
	}
	class MIME : DecodeMethod
	{
		bool headerParsed=false;
		bool dataStarted=false;
		bool dataEnd=false;
		public static bool MethodFound(string l, int partNumber)		
		{	// This is a multi-part message in MIME format
            return l.IndexOf("MIME", StringComparison.InvariantCulture) > 0; //.StartsWith("This is a multi-part message in MIME format");
		}
		public MIME(StringWriter err, int partNum) : base(Method.uuEncode, partNum, err) { }
		public override byte[] DecodeDataLine(string l, int lineNum)
		{
			if(dataEnd)
				return null;
			if(!headerParsed)
			{
				headerParsed=ParseHeader(l);
				return null;
			}
			if(headerParsed && ParseNextPart(l))
			{
				dataEnd=true;
				return null;
			}
			if(!dataStarted)
				dataStarted=FirstDataLine(l);
			if(!dataStarted)
				return null;
			return Convert.FromBase64String(l);
		}
		bool FirstDataLine(string l)			
		{
			return l.StartsWith("/9j/");
		}	
		bool ParseHeader(string l)			
		{
			dataName=GetStringField(l, "name=");
			dataName=dataName.Trim(new char[]{'\"'});
			return dataName.Length>0;
		}	
		bool ParseNextPart(string l)
		{
			return l[0]=='-' && l.IndexOf("_NextPart_")>0;
		}
	}
}