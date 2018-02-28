using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SLD.Tezos.Client
{
	using Security;
	using Model;

	using System.Threading.Tasks;
	using SLD.Tezos.Protocol;

	[TestClass]
	public class ApprovalTest : ClientTest
	{
		const string Right = "Right";
		const string Wrong = "Wrong";

		Identity manager;

		[TestInitialize]
		public async Task BeforeEach()
		{
			await ConnectToSimulation();

			manager = await Engine.AddIdentity("Stereotype", "Identity", Right);
		}


		[TestMethod]
		public async Task Approval_Process()
		{
			Approval approval = null;
			SigningResult result = SigningResult.Pending;

			Engine.ApprovalRequired += async a =>
			{
				approval = a;
				Assert.IsFalse(a.IsApproved);

				a.PassphraseText = Right;
				result = await a.Approve(true);
				Assert.IsTrue(a.IsApproved);
			};

			var task = await Engine.CreateAccount("Account", manager, manager, 100);

			Assert.IsNotNull(approval);
			Assert.AreSame(task, approval.Task);
			Assert.AreSame(manager, approval.Signer);
			Assert.IsNull(approval.LastError);
			Assert.IsTrue(approval.IsApproved);
			Assert.AreEqual(SigningResult.Signed, result);
		}

		[TestMethod]
		public async Task Approval_Cancel()
		{
			Approval approval = null;
			SigningResult result = SigningResult.Pending;

			Engine.ApprovalRequired += async a =>
			{
				approval = a;
				result = await a.Approve(false);
				Assert.IsFalse(a.IsApproved);
			};

			var task = await Engine.CreateAccount("Account", manager, manager, 100);

			Assert.IsNull(approval.LastError);
			Assert.IsFalse(approval.IsApproved);
			Assert.AreEqual(SigningResult.Cancelled, result);
		}

		[TestMethod]
		public async Task Approval_Timeout()
		{
			Engine.ApprovalTimeout = TimeSpan.FromMilliseconds(100);

			Approval approval = null;

			Engine.ApprovalRequired += a =>
			{
				approval = a;
			};

			var task = await Engine.CreateAccount("Account", manager, manager, 100);

			Assert.IsNotNull(approval);
			Assert.AreEqual(SigningResult.Timeout, approval.Result);
		}

		[TestMethod]
		public async Task Approval_PasswordFailure()
		{
			Approval approval = null;
			SigningResult result = SigningResult.Pending;

			Engine.ApprovalRequired += async a =>
			{
				approval = a;

				a.PassphraseText = Wrong;

				result = await a.Approve(true);

				Assert.AreEqual(SigningResult.InvalidCredentials, result);
				Assert.AreEqual(SigningResult.Pending, approval.Result);

				result = await a.Approve(false);
			};

			var task = await Engine.CreateAccount("Account", manager, manager, 100);

			Assert.AreEqual(SigningResult.Cancelled, result);
		}

	}
}
