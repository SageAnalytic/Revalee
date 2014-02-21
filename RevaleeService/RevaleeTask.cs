using System;
using System.Threading;

namespace RevaleeService
{
	public class RevaleeTask : IComparable<RevaleeTask>
	{
		private readonly DateTime _CallbackTime;
		private readonly Uri _CallbackUrl;
		private readonly DateTime _CreatedTime;
		private readonly Guid _CallbackId;
		private readonly string _AuthorizationCipher;
		private int _AttemptsRemaining;

		public RevaleeTask(DateTime callbackTime, Uri callbackUrl)
			: this(callbackTime, callbackUrl, 0, null)
		{
		}

		public RevaleeTask(DateTime callbackTime, Uri callbackUrl, int retryCount)
			: this(callbackTime, callbackUrl, retryCount, null)
		{
		}

		public RevaleeTask(DateTime callbackTime, Uri callbackUrl, int retryCount, string authorizationCipher)
		{
			_CreatedTime = DateTime.UtcNow;

			if (callbackUrl == null)
			{
				throw new ArgumentNullException("callbackUrl");
			}

			_CallbackTime = callbackTime;
			_CallbackUrl = callbackUrl;
			_CallbackId = Guid.NewGuid();

			if (retryCount < 1)
			{
				_AttemptsRemaining = 1;
			}
			else
			{
				_AttemptsRemaining = 1 + retryCount;
			}

			if (!string.IsNullOrEmpty(authorizationCipher))
			{
				_AuthorizationCipher = authorizationCipher;
			}
		}

		private RevaleeTask(DateTime callbackTime, Uri callbackUrl, DateTime createdTime, Guid callbackId, int attemptsRemaining, string authorizationCipher)
		{
			if (callbackUrl == null)
			{
				throw new ArgumentNullException("callbackUrl");
			}

			if (Guid.Empty.Equals(callbackId))
			{
				throw new ArgumentNullException("callbackId");
			}

			_CallbackTime = callbackTime;
			_CallbackUrl = callbackUrl;
			_CreatedTime = createdTime;
			_CallbackId = callbackId;

			if (attemptsRemaining < 0)
			{
				_AttemptsRemaining = 0;
			}
			else
			{
				_AttemptsRemaining = attemptsRemaining;
			}

			if (!string.IsNullOrEmpty(authorizationCipher))
			{
				_AuthorizationCipher = authorizationCipher;
			}
		}

		public static RevaleeTask Revive(DateTime callbackTime, Uri callbackUrl, DateTime createdTime, Guid callbackId, int attemptsRemaining, string authorizationCipher)
		{
			return new RevaleeTask(callbackTime, callbackUrl, createdTime, callbackId, attemptsRemaining, authorizationCipher);
		}

		public DateTime CallbackTime
		{
			get { return _CallbackTime; }
		}

		public Uri CallbackUrl
		{
			get { return _CallbackUrl; }
		}

		public DateTime CreatedTime
		{
			get { return _CreatedTime; }
		}

		public Guid CallbackId
		{
			get { return _CallbackId; }
		}

		public int AttemptsRemaining
		{
			get { return _AttemptsRemaining; }
		}

		public string AuthorizationCipher
		{
			get { return _AuthorizationCipher; }
		}

		public bool AttemptCallback()
		{
			return Interlocked.Decrement(ref _AttemptsRemaining) >= 0;
		}

		public int CompareTo(RevaleeTask other)
		{
			if (_CallbackId.Equals(other.CallbackId))
			{
				return 0;
			}

			int returnValue = _CallbackTime.CompareTo(other.CallbackTime);

			if (returnValue == 0)
			{
				returnValue = _CreatedTime.CompareTo(other.CreatedTime);
				if (returnValue == 0)
				{
					returnValue = -1;
				}
			}

			return returnValue;
		}
	}
}