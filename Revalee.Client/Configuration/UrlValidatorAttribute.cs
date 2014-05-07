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
using System.Configuration;

namespace Revalee.Client.Configuration
{
	internal sealed class UrlValidatorAttribute : ConfigurationValidatorAttribute
	{
		private bool _AllowAbsolute = true;

		private bool _AllowRelative = true;

		public UrlValidatorAttribute()
		{
		}

		public bool AllowAbsolute
		{
			get
			{
				return _AllowAbsolute;
			}
			set
			{
				_AllowAbsolute = value;
			}
		}

		public bool AllowRelative
		{
			get
			{
				return _AllowRelative;
			}
			set
			{
				_AllowRelative = value;
			}
		}

		public override ConfigurationValidatorBase ValidatorInstance
		{
			get
			{
				return new UrlValidator(_AllowAbsolute, _AllowRelative);
			}
		}

		private class UrlValidator : ConfigurationValidatorBase
		{
			private bool _AllowAbsolute = true;
			private bool _AllowRelative = true;

			public UrlValidator(bool allowAbsolute, bool allowRelative)
			{
				_AllowAbsolute = allowAbsolute;
				_AllowRelative = allowRelative;
			}

			public override bool CanValidate(Type type)
			{
				return type == typeof(Uri);
			}

			public override void Validate(object value)
			{
				if (value == null)
				{
					return;
				}

				if (value.GetType() != typeof(Uri))
				{
					throw new ArgumentException("The URL attribute is invalid.");
				}

				Uri url = value as Uri;

				if (!_AllowAbsolute && url.IsAbsoluteUri)
				{
					throw new ArgumentException("The URL attribute cannot contain an absolute URL.");
				}

				if (!_AllowRelative && !url.IsAbsoluteUri)
				{
					throw new ArgumentException("The URL attribute cannot contain a relative URL.");
				}

				if (url.IsAbsoluteUri && url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
				{
					throw new ArgumentException(string.Format("The URL attribute only supports {0} and {1}.", Uri.UriSchemeHttp, Uri.UriSchemeHttps));
				}
			}
		}
	}
}