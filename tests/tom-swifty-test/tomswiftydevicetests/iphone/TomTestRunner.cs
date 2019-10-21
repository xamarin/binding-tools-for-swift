using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace TomTest
{
	public class TomTestRunner
	{
		private UITextView _textOutput;
		private XmlWriter _writer;
		private int _testCount, _passCount, _failCount, _skipCount;

		public TomTestRunner (UITextView textOutput, TextWriter writer)
		{
			_textOutput = textOutput;
			string args = Environment.CommandLine;
			XmlWriterSettings settings = new XmlWriterSettings ();
			settings.Indent = true;
			settings.NewLineOnAttributes = true;
			_writer = XmlWriter.Create (writer, settings);
		}

		public void RunAll (IEnumerable<Assembly> assemblies)
		{
			_testCount = _passCount = _failCount = _skipCount = 0;
			List<Type> allTestTypes = TestClasses (assemblies).OrderBy (t => t.Name).ToList ();
			_writer.WriteStartElement ("tests");
			foreach (Type t in allTestTypes) {
				// can't be null - see IsATestClass
				ConstructorInfo ci = t.GetConstructor (new Type [0]);
				ITomTest test = (ITomTest) ci.Invoke (null);
				Run (test);
			}
			_writer.WriteEndElement ();
			AppendText ($"Executed {_testCount}. {_passCount} passed, {_failCount} failed, {_skipCount} skipped.\n");
			TestsDone (this, new EventArgs ());
		}

		IEnumerable<Type> TestClasses (IEnumerable<Assembly> assemblies)
		{
			foreach (var assembly in assemblies) {
				AssemblyName name = assembly.GetName ();
				string shortName = name.Name;
				if (shortName.StartsWith ("System", StringComparison.Ordinal) || shortName.StartsWith ("MonoTouch", StringComparison.Ordinal) ||
					shortName.StartsWith ("Xamarin", StringComparison.Ordinal)) {
					continue;
				}
				Type [] types = assembly.GetTypes ();
				foreach (var type in types) {
					if (IsATestClass (type)) {
						yield return type;
					}
				}
			}
		}

		public event EventHandler<EventArgs> TestsDone = (s, e) => { };

		static bool IsATestClass (Type t)
		{
			return t.IsClass && typeof (ITomTest).IsAssignableFrom (t);
		}

		public void Run (ITomTest test)
		{
			string name = test.TestName;
			string expected = test.ExpectedOutput;
			TestStarted (name);
			try {
				TomSkipAttribute attr = test.GetType ().GetCustomAttribute<TomSkipAttribute> ();
				if (attr != null) {
					TestSkipped (attr.Reason);
				} else {
					int startTime = Environment.TickCount;
					TextWriter oldWriter = Console.Out;
					StringWriter newOut = new StringWriter ();
					Console.SetOut (newOut);
					try {
						test.Run ();
					} finally {
						Console.SetOut (oldWriter);
					}
					int endTime = Environment.TickCount;
					TestCompleted (expected, newOut.ToString (), endTime - startTime);
				}
			} catch (Exception e) {
				TestFailed (e);
			}

			_writer.Flush ();
		}

		void TestStarted (string testName)
		{
			_testCount++;
			AppendText ($"{testName}: ");
			_writer.WriteStartElement ("test");
			_writer.WriteAttributeString ("name", testName);
			_writer.Flush ();
		}

		void TestCompleted (string expected, string actual, int ticks)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ($"({ticks} ms) ");
			if (expected != actual) {
				_failCount++;
				_writer.WriteStartElement ("fail");
				_writer.WriteAttributeString ("reason", "badoutput");
				_writer.WriteAttributeString ("time", ticks.ToString ());
				_writer.WriteElementString ("expected", expected);
				_writer.WriteElementString ("actual", actual);
				_writer.WriteEndElement ();

				sb.Append ("fail:\n");
				sb.Append ($"expected: \"{expected}\"\n");
				sb.Append ($"actual: \"{actual}\"\n");
			} else {
				_passCount++;
				_writer.WriteStartElement ("pass");
				_writer.WriteAttributeString ("time", ticks.ToString ());
				_writer.WriteEndElement ();
				sb.Append ("pass\n");
			}
			_writer.WriteEndElement ();
			AppendText (sb.ToString ());
		}

		void TestFailed (Exception e)
		{
			_failCount++;
			AppendText ($"fail: {e.Message}\n");
			_writer.WriteStartElement ("fail");
			_writer.WriteAttributeString ("reason", "exception");
			_writer.WriteAttributeString ("message", e.Message);
			_writer.WriteAttributeString ("class", e.GetType ().Name);
			_writer.WriteAttributeString ("backtrace", e.StackTrace);
			_writer.WriteEndElement ();
			_writer.WriteEndElement ();
			_writer.Flush ();
		}

		void TestSkipped (string reason)
		{
			_skipCount++;
			AppendText ($"skipped: {reason}\n");
			_writer.WriteStartElement ("skipped");
			_writer.WriteAttributeString ("message", reason);
			_writer.WriteEndElement ();
			_writer.WriteEndElement ();
			_writer.Flush ();
		}

		void AppendText (string text)
		{
			Console.Write (text);
			_textOutput.InvokeOnMainThread (() => {
				_textOutput.Text = _textOutput.Text + text;
				var bounds = _textOutput.GetCaretRectForPosition (_textOutput.SelectedTextRange.Start);
				_textOutput.ScrollRectToVisible (bounds, false);
			});
		}


	}


	//public class MockTomTest : ITomTest
	//{
	//    public void Run()
	//    {
	//        Thread.Sleep(2000);
	//        Console.WriteLine("output");
	//    }
	//    public string TestName { get { return "Mock Test"; } }
	//    public string ExpectedOutput { get { return "output\n"; } }
	//}
}
