#region Copyright & License

// Copyright © 2012 - 2020 François Chabot
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
using System.Diagnostics.CodeAnalysis;
using Be.Stateless.BizTalk.Settings.Sso;
using FluentAssertions;
using Xunit;

namespace Be.Stateless.BizTalk.Runtime.Caching
{
	public class AffiliateApplicationCacheFixture
	{
		[Fact]
		[SuppressMessage("ReSharper", "NotAccessedVariable")]
		public void CacheThrowsIfAffiliateApplicationDoesNotExist()
		{
			const string name = "NonexistentApplication";
			AffiliateApplication application;
			Action act = () => application = AffiliateApplicationCache.Instance[name];
			act.Should()
				.Throw<InvalidOperationException>()
				.WithMessage($"{nameof(AffiliateApplication)} '{name}' does not exist.");
		}
	}
}
