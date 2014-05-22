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

using Revalee.Client.Configuration;
using System;
using System.ComponentModel;
using System.Configuration;

namespace Revalee.Client.RecurringTasks
{
	internal class TaskConfigurationElement : ConfigurationElement
	{
		public TaskConfigurationElement()
		{
			this.Key = Guid.NewGuid();
		}

		internal Guid Key { get; private set; }

		[ConfigurationProperty("periodicity", IsKey = false, IsRequired = true)]
		[TypeConverter(typeof(EnumConfigurationConverter<PeriodicityType>))]
		public PeriodicityType Periodicity
		{
			get
			{
				return (PeriodicityType)this["periodicity"];
			}
		}

		[ConfigurationProperty("hour", DefaultValue = 0, IsKey = false, IsRequired = false)]
		[IntegerValidator(ExcludeRange = false, MinValue = 0, MaxValue = 23)]
		public int Hour
		{
			get
			{
				return (int)this["hour"];
			}
		}

		[ConfigurationProperty("minute", IsKey = false, IsRequired = true)]
		[IntegerValidator(ExcludeRange = false, MinValue = 0, MaxValue = 59)]
		public int Minute
		{
			get
			{
				return (int)this["minute"];
			}
		}

		[ConfigurationProperty("url", IsKey = false, IsRequired = true)]
		[UrlValidator(AllowAbsolute = true, AllowRelative = true)]
		public Uri Url
		{
			get
			{
				return (Uri)this["url"];
			}
		}
	}
}