#region License

/*
The MIT License (MIT)

Copyright (c) 2014 Sage Analytic Technologies, LLC

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#endregion License

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Revalee.Client
{
	/// <summary>
	/// Represents errors that occur during the processing of a Revalee service request.
	/// </summary>
	[Serializable]
	public class RevaleeRequestException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Revalee.Client.RevaleeRequestException"/> class.
		/// </summary>
		/// <param name="serviceBaseUri">A <see cref="T:System.Uri"/> representing the scheme, host, and port for the Revalee service (example: http://localhost:46200).</param>
		/// <param name="callbackUri">An absolute <see cref="T:System.Uri"/> that will be requested on the callback.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public RevaleeRequestException(Uri serviceBaseUri, Uri callbackUri, Exception innerException)
			: base("The request to the Revalee service was unsuccessful.", innerException)
		{
			this.ServiceBaseUri = serviceBaseUri;
			this.CallbackUri = callbackUri;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Revalee.Client.RevaleeRequestException"/> class with serialized data.
		/// </summary>
		/// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
		protected RevaleeRequestException(SerializationInfo info, StreamingContext context)
			: base(info,context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}

			this.ServiceBaseUri = new Uri(info.GetString("ServiceBaseUri"));
			this.CallbackUri = new Uri(info.GetString("CallbackUri"));
		}

		/// <summary>Gets the service base Uri used to make this Revalee service request.</summary>
		/// <returns>The service base Uri used to make this Revalee service request.</returns>
		public Uri ServiceBaseUri { get; private set; }

		/// <summary>Gets the callback Uri used to make this Revalee service request.</summary>
		/// <returns>The callback Uri used to make this Revalee service request.</returns>
		public Uri CallbackUri { get; private set; }

		/// <summary>
		/// Sets the <see cref="T:System.Runtime.Serialization.SerializationInfo" /> with information about the exception.
		/// </summary>
		/// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}

			base.GetObjectData(info, context);

			info.AddValue("ServiceBaseUri", this.ServiceBaseUri.ToString());
			info.AddValue("CallbackUri", this.CallbackUri.ToString());
		}
	}
}