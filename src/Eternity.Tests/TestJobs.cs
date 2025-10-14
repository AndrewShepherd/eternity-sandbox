namespace Eternity.Tests;

using static Eternity.Tests.Samples;

[TestClass]
public sealed class TestJobs
{
	[TestMethod]
	public void TenJobs()
	{
		var pieces = PieceTextReader.Parse(source_6x6);
		Assert.AreEqual(pieces.Length, 36);

		PartiallyExploredTreeNode treeNode = (PartiallyExploredTreeNode)StackEntryExtensions.CreateInitialStack(pieces);

		(TreeNode newNode, var paths) = Job.DivideIntoJobs(treeNode, 10);
		Assert.AreEqual(10, paths.Count());
	}
}
