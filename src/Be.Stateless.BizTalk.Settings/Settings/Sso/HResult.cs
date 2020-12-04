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

namespace Be.Stateless.BizTalk.Settings.Sso
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32")]
	internal enum HResult : uint
	{
		// Error Code = 'The application does not exist.'
		ErrorApplicationNonExistent = 0xC0002A04,

		// Error Code = 'The mapping does not exist. For Config Store applications, the config info has not been set.'
		ErrorMappingNonExistent = 0xC0002A05,

		// Error Code = 'The external credentials in the SSO database are more recent.'
		ErrorSsoDbExternalCredentialsAreMoreRecent = 0xC0002A40
	}
}
