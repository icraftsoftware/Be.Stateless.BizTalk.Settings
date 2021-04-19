#region Copyright & License

// Copyright © 2012 - 2021 François Chabot
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using Be.Stateless.BizTalk.Settings.Sso;

namespace Be.Stateless.BizTalk.Runtime.Caching
{
	/// <summary>
	/// Runtime sliding memory cache for the <see cref="AffiliateApplication"/>s.
	/// </summary>
	/// <seealso cref="Cache{TKey,TItem}"/>
	/// <seealso cref="SlidingCache{TKey,TItem}"/>
	public class AffiliateApplicationCache : SlidingCache<string, AffiliateApplication>
	{
		/// <summary>
		/// Singleton <see cref="AffiliateApplicationCache"/> instance.
		/// </summary>
		public static AffiliateApplicationCache Instance { get; } = new();

		/// <summary>
		/// Create the singleton <see cref="AffiliateApplicationCache"/> instance.
		/// </summary>
		private AffiliateApplicationCache() : base(
			key => key,
			key => AffiliateApplication.FindByName(key) ?? throw new InvalidOperationException($"{nameof(AffiliateApplication)} '{key}' does not exist.")) { }
	}
}
