using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace RevaleeService
{
	public class RollingLogFileTraceListener : TraceListener
	{
		private string _LogFile;
		private TextWriter _Writer;
		private DateTime _CurrentLogDay;

		private readonly bool _IsRolling;
		private readonly object _SyncRoot = new object();

		public RollingLogFileTraceListener()
		{
			_LogFile = DetermineDefaultLogFilename();
			_IsRolling = true;
		}

		public RollingLogFileTraceListener(Stream stream)
			: this(stream, string.Empty)
		{
		}

		public RollingLogFileTraceListener(Stream stream, string name)
			: base(name)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}

			_Writer = new StreamWriter(stream, GetEncodingWithFallback(new UTF8Encoding(false)), 4096);
			_IsRolling = false;
		}

		public RollingLogFileTraceListener(TextWriter writer)
			: this(writer, string.Empty)
		{
		}

		public RollingLogFileTraceListener(TextWriter writer, string name)
			: base(name)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}

			_Writer = writer;
			_IsRolling = false;
		}

		public RollingLogFileTraceListener(string filename)
			: this(filename, string.Empty)
		{
		}

		public RollingLogFileTraceListener(string filename, string name)
			: base(name)
		{
			_LogFile = filename;
			_IsRolling = true;
		}

		public override void Close()
		{
			if (_Writer != null)
			{
				try
				{
					_Writer.Close();
				}
				catch (ObjectDisposedException)
				{
				}
			}

			_Writer = null;
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (!disposing)
				{
					if (_Writer != null)
					{
						try
						{
							_Writer.Close();
						}
						catch (ObjectDisposedException)
						{
						}
					}

					_Writer = null;
				}
				else
				{
					Close();
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		private void RolloverLogFilename()
		{
			lock (_SyncRoot)
			{
				if (_Writer != null)
				{
					if (_CurrentLogDay == DateTime.Today)
					{
						return;
					}

					_Writer.Close();
				}

				_CurrentLogDay = DateTime.Today;
				string rollingLogFilename = DetermineRollingLogFilename(_LogFile, _CurrentLogDay);
				_Writer = new StreamWriter(rollingLogFilename, true, GetEncodingWithFallback(new UTF8Encoding(false)), 4096);
			}
		}

		private bool EnsureWriter()
		{
			if (_IsRolling)
			{
				if (_Writer != null && _CurrentLogDay == DateTime.Today)
				{
					return true;
				}

				RolloverLogFilename();
				return true;
			}
			else
			{
				if (_Writer != null)
				{
					return true;
				}
				else
				{
					if (_LogFile != null)
					{
						_Writer = new StreamWriter(_LogFile, true, GetEncodingWithFallback(new UTF8Encoding(false)), 4096);
						return true;
					}

					return false;
				}
			}
		}

		public override void Flush()
		{
			if (_Writer != null)
			{
				try
				{
					_Writer.Flush();
				}
				catch (ObjectDisposedException)
				{
				}
				return;
			}
			else
			{
				return;
			}
		}

		private static Encoding GetEncodingWithFallback(Encoding encoding)
		{
			Encoding replacementFallback = (Encoding)encoding.Clone();
			replacementFallback.EncoderFallback = EncoderFallback.ReplacementFallback;
			replacementFallback.DecoderFallback = DecoderFallback.ReplacementFallback;
			return replacementFallback;
		}

		private static string DetermineRollingLogFilename(string baseFilename, DateTime date)
		{
			if (!Path.IsPathRooted(baseFilename))
			{
				baseFilename = Path.Combine(DetermineLogFileRoot(), baseFilename);
			}

			string baseFilenameWithoutExtension = Path.GetFileNameWithoutExtension(baseFilename);
			string baseFilenameExtension = Path.GetExtension(baseFilename);

			if (string.IsNullOrWhiteSpace(baseFilenameExtension))
			{
				baseFilenameExtension = ".log";
			}

			return Path.Combine(Path.GetDirectoryName(baseFilename), string.Format("{0}.{1:yyyy-MM-dd}{2}", baseFilenameWithoutExtension, date, baseFilenameExtension));
		}

		private static string DetermineLogFileRoot()
		{
			return ApplicationFolderHelper.ApplicationFolderName;
		}

		private static string DetermineDefaultLogFilename()
		{
			return string.Concat(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location), ".log");
		}

		public override void Write(string message)
		{
			if (EnsureWriter())
			{
				if (base.NeedIndent)
				{
					this.WriteIndent();
				}

				try
				{
					_Writer.Write(message);
				}
				catch (ObjectDisposedException)
				{
				}

				return;
			}
			else
			{
				return;
			}
		}

		public override void WriteLine(string message)
		{
			if (EnsureWriter())
			{
				if (base.NeedIndent)
				{
					this.WriteIndent();
				}

				try
				{
					_Writer.Write(string.Format("[{0}] ", DateTimeOffset.Now));
					_Writer.WriteLine(message);
					base.NeedIndent = true;
				}
				catch (ObjectDisposedException)
				{
				}

				return;
			}
			else
			{
				return;
			}
		}
	}
}