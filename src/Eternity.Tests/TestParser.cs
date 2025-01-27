using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eternity.Tests
{
	[TestClass]
	public sealed class TestParser
	{
		public const string SourceWithCapitals = """
			"hixx","kixx","hcxx","lfxx","Efxl","Gjxe","okxj","Fkxb","qhxk","rbxk"
			,"Bkxa","Iaxg","Bdxh","oexf","vgxa","ugxi","Ocxg","Bbxb","Gfxa","texg"
			,"sexc","Rjxi","oaxd","rlxb","njxa","Rexa","Khxc","Dkxg","Jjxb","vbxe"
			,"Olxi","ugxe","Gexf","Jdxb","Fhxe","Ehxj","Mbxd","rgxd","tgxj","Jixl"
			,"udxc","Qcxk","scxi","Olxj","Djxd","Ldxh","tfxd","Qfxc","Hixl","maxc"
			,"Llxf","oixf","Rcxl","oaxg","Ahxk","pdxi","Dlxh","Bbxf","Naxe","Kkxj"
			,"ssDp","QKKE","IQuo","rHAF","GODp","DQno","oJRP","rCHq","pmIs","rOtL"
			,"vpnm","tsIJ","ICQC","NHAP","LHFH","FmFq","GEmJ","nNBD","qMLJ","DGDu"
			,"vPMn","BECF","PFom","RRNM","LQPt","nruF","CJKC","EOJG","FInv","uRoH"
			,"PILA","MMsK","QvCN","qKRs","pHsn","AEGL","JDCH","FQHJ","rmCG","COnP"
			,"tDQB","NKEL","upAJ","LqIH","sqNM","FHMR","AAns","MouK","HKQn","nPnB"
			,"pKvq","CpLL","OsAo","qBNA","BvIm","qKGs","HFrQ","qELF","sMuD","DqHn"
			,"PRPt","IQEm","PBoB","HnLO","IEJN","tFBP","qKtD","pmmu","qIDp","rnMK"
			,"rmIp","oOrM","NIII","JArq","FQQD","IJJB","sACE","DFQN","AMvn","mAAt"
			,"AHGD","GIpI","tnrB","voFq","MJuL","PCDv","RBop","vGPr","vpMp","AoEQ"
			,"BQMQ","sorG","nvHQ","JDuI","QORE","QvpR","Gvnv","NLIM","FsGH","EAND"
			,"EPCD","AJPN","OKFC","Koqs","MHnt","GKHm","MnFP","IHJI","oRCq","RCBP"
			,"vEBL","mBKP","sQKP","rGEP","tApP","GsuA","PuCt","FpID","KqNH","BpKK"
			,"OMqs","NrOL","KqRN","prPC","AEmu","qCRs","GtsK","EMLm","ORLR","Fmto"
			,"QOsG","GQoM","tJEF","PNLO","Jumu","uKJB","Hptu","EFEA","toms","QAuq"
			,"qKOt","BmIs","PCrR","Mntr","CoqN","Cvos","NpRB","rLCm","PFPJ","JNCL"
			,"nAvR","MFAu","FIuL","vOmK","DvmG","pGJL","suRM","mrNN","mQIt","CRpo"
			,"pEmM","LvRQ","BptQ","NHGO","JJBO","MEqo","tAQG","rBtu","LMuE","Kqrr"
			,"GnoD","NHru","EODA","PvGC","RvEu","OqLK","rpCp","AnmL","mCJM","HvuB"
			,"HNBF","ONLO","sPIR","RNMO","pDHN","COsv","oqBu","KGJv","IArR","DHqD"
			,"nNrv","Enon","tREm","tOvO","OtIG","DntF"
			
			""";

		[TestMethod]
		public void ParseCapitals()
		{
			var pieces = PieceTextReader.Parse(SourceWithCapitals);
			Assert.AreEqual(256, pieces.Length);

			Placements? placements = Placements.CreateInitial(pieces);
		}
	}
}
