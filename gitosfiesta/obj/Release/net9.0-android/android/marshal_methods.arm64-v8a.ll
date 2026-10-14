; ModuleID = 'marshal_methods.arm64-v8a.ll'
source_filename = "marshal_methods.arm64-v8a.ll"
target datalayout = "e-m:e-i8:8:32-i16:16:32-i64:64-i128:128-n32:64-S128"
target triple = "aarch64-unknown-linux-android21"

%struct.MarshalMethodName = type {
	i64, ; uint64_t id
	ptr ; char* name
}

%struct.MarshalMethodsManagedClass = type {
	i32, ; uint32_t token
	ptr ; MonoClass klass
}

@assembly_image_cache = dso_local local_unnamed_addr global [149 x ptr] zeroinitializer, align 8

; Each entry maps hash of an assembly name to an index into the `assembly_image_cache` array
@assembly_image_cache_hashes = dso_local local_unnamed_addr constant [447 x i64] [
	i64 u0x0071cf2d27b7d61e, ; 0: lib_Xamarin.AndroidX.SwipeRefreshLayout.dll.so => 88
	i64 u0x02123411c4e01926, ; 1: lib_Xamarin.AndroidX.Navigation.Runtime.dll.so => 84
	i64 u0x0297093beda3df86, ; 2: Supabase.Gotrue.dll => 58
	i64 u0x02abedc11addc1ed, ; 3: lib_Mono.Android.Runtime.dll.so => 147
	i64 u0x032267b2a94db371, ; 4: lib_Xamarin.AndroidX.AppCompat.dll.so => 67
	i64 u0x033a1d0324ba06bd, ; 5: Microsoft.IO.RecyclableMemoryStream.dll => 48
	i64 u0x043032f1d071fae0, ; 6: ru/Microsoft.Maui.Controls.resources => 24
	i64 u0x044440a55165631e, ; 7: lib-cs-Microsoft.Maui.Controls.resources.dll.so => 2
	i64 u0x046eb1581a80c6b0, ; 8: vi/Microsoft.Maui.Controls.resources => 30
	i64 u0x0470607fd33c32db, ; 9: Microsoft.IdentityModel.Abstractions.dll => 44
	i64 u0x0517ef04e06e9f76, ; 10: System.Net.Primitives => 117
	i64 u0x0565d18c6da3de38, ; 11: Xamarin.AndroidX.RecyclerView => 86
	i64 u0x0581db89237110e9, ; 12: lib_System.Collections.dll.so => 99
	i64 u0x05989cb940b225a9, ; 13: Microsoft.Maui.dll => 51
	i64 u0x06076b5d2b581f08, ; 14: zh-HK/Microsoft.Maui.Controls.resources => 31
	i64 u0x0680a433c781bb3d, ; 15: Xamarin.AndroidX.Collection.Jvm => 70
	i64 u0x07c57877c7ba78ad, ; 16: ru/Microsoft.Maui.Controls.resources.dll => 24
	i64 u0x07dcdc7460a0c5e4, ; 17: System.Collections.NonGeneric => 97
	i64 u0x08f3c9788ee2153c, ; 18: Xamarin.AndroidX.DrawerLayout => 75
	i64 u0x0919c28b89381a0b, ; 19: lib_Microsoft.Extensions.Options.dll.so => 42
	i64 u0x092266563089ae3e, ; 20: lib_System.Collections.NonGeneric.dll.so => 97
	i64 u0x09d144a7e214d457, ; 21: System.Security.Cryptography => 134
	i64 u0x0abb3e2b271edc45, ; 22: System.Threading.Channels.dll => 138
	i64 u0x0b3b632c3bbee20c, ; 23: sk/Microsoft.Maui.Controls.resources => 25
	i64 u0x0b6aff547b84fbe9, ; 24: Xamarin.KotlinX.Serialization.Core.Jvm => 94
	i64 u0x0be2e1f8ce4064ed, ; 25: Xamarin.AndroidX.ViewPager => 89
	i64 u0x0c3ca6cc978e2aae, ; 26: pt-BR/Microsoft.Maui.Controls.resources => 21
	i64 u0x0c59ad9fbbd43abe, ; 27: Mono.Android => 148
	i64 u0x0c7790f60165fc06, ; 28: lib_Microsoft.Maui.Essentials.dll.so => 52
	i64 u0x0d518d16a10d1bcf, ; 29: Supabase.Functions => 57
	i64 u0x0e14e73a54dda68e, ; 30: lib_System.Net.NameResolution.dll.so => 114
	i64 u0x0e2e96803ecb3446, ; 31: Supabase.Storage => 61
	i64 u0x0fc6e5711dabb83e, ; 32: lib_Supabase.Realtime.dll.so => 60
	i64 u0x102a31b45304b1da, ; 33: Xamarin.AndroidX.CustomView => 74
	i64 u0x10f6cfcbcf801616, ; 34: System.IO.Compression.Brotli => 107
	i64 u0x11a70d0e1009fb11, ; 35: System.Net.WebSockets.dll => 123
	i64 u0x125b7f94acb989db, ; 36: Xamarin.AndroidX.RecyclerView.dll => 86
	i64 u0x13a01de0cbc3f06c, ; 37: lib-fr-Microsoft.Maui.Controls.resources.dll.so => 8
	i64 u0x13f1e5e209e91af4, ; 38: lib_Java.Interop.dll.so => 146
	i64 u0x13f1e880c25d96d1, ; 39: he/Microsoft.Maui.Controls.resources => 9
	i64 u0x143d8ea60a6a4011, ; 40: Microsoft.Extensions.DependencyInjection.Abstractions => 39
	i64 u0x1497051b917530bd, ; 41: lib_System.Net.WebSockets.dll.so => 123
	i64 u0x17125c9a85b4929f, ; 42: lib_netstandard.dll.so => 144
	i64 u0x17b56e25558a5d36, ; 43: lib-hu-Microsoft.Maui.Controls.resources.dll.so => 12
	i64 u0x17f9358913beb16a, ; 44: System.Text.Encodings.Web => 135
	i64 u0x18402a709e357f3b, ; 45: lib_Xamarin.KotlinX.Serialization.Core.Jvm.dll.so => 94
	i64 u0x18f0ce884e87d89a, ; 46: nb/Microsoft.Maui.Controls.resources.dll => 18
	i64 u0x19a4c090f14ebb66, ; 47: System.Security.Claims => 133
	i64 u0x1a91866a319e9259, ; 48: lib_System.Collections.Concurrent.dll.so => 96
	i64 u0x1aac34d1917ba5d3, ; 49: lib_System.dll.so => 143
	i64 u0x1aad60783ffa3e5b, ; 50: lib-th-Microsoft.Maui.Controls.resources.dll.so => 27
	i64 u0x1aea8f1c3b282172, ; 51: lib_System.Net.Ping.dll.so => 116
	i64 u0x1c753b5ff15bce1b, ; 52: Mono.Android.Runtime.dll => 147
	i64 u0x1e3d87657e9659bc, ; 53: Xamarin.AndroidX.Navigation.UI => 85
	i64 u0x1e57ec3104eb59d9, ; 54: lib_Supabase.Gotrue.dll.so => 58
	i64 u0x1e71143913d56c10, ; 55: lib-ko-Microsoft.Maui.Controls.resources.dll.so => 16
	i64 u0x1ed8fcce5e9b50a0, ; 56: Microsoft.Extensions.Options.dll => 42
	i64 u0x1f8968eb274e3a08, ; 57: gitosfiesta.dll => 95
	i64 u0x209375905fcc1bad, ; 58: lib_System.IO.Compression.Brotli.dll.so => 107
	i64 u0x20aa4eb4c5cf3260, ; 59: Supabase.Realtime.dll => 60
	i64 u0x2174319c0d835bc9, ; 60: System.Runtime => 132
	i64 u0x220fd4f2e7c48170, ; 61: th/Microsoft.Maui.Controls.resources => 27
	i64 u0x237be844f1f812c7, ; 62: System.Threading.Thread.dll => 139
	i64 u0x2407aef2bbe8fadf, ; 63: System.Console => 103
	i64 u0x240abe014b27e7d3, ; 64: Xamarin.AndroidX.Core.dll => 72
	i64 u0x247619fe4413f8bf, ; 65: System.Runtime.Serialization.Primitives.dll => 131
	i64 u0x252073cc3caa62c2, ; 66: fr/Microsoft.Maui.Controls.resources.dll => 8
	i64 u0x2662c629b96b0b30, ; 67: lib_Xamarin.Kotlin.StdLib.dll.so => 92
	i64 u0x268c1439f13bcc29, ; 68: lib_Microsoft.Extensions.Primitives.dll.so => 43
	i64 u0x270a44600c921861, ; 69: System.IdentityModel.Tokens.Jwt => 63
	i64 u0x273f3515de5faf0d, ; 70: id/Microsoft.Maui.Controls.resources.dll => 13
	i64 u0x2742545f9094896d, ; 71: hr/Microsoft.Maui.Controls.resources => 11
	i64 u0x2759af78ab94d39b, ; 72: System.Net.WebSockets => 123
	i64 u0x27b410442fad6cf1, ; 73: Java.Interop.dll => 146
	i64 u0x2801845a2c71fbfb, ; 74: System.Net.Primitives.dll => 117
	i64 u0x2a128783efe70ba0, ; 75: uk/Microsoft.Maui.Controls.resources.dll => 29
	i64 u0x2a3b095612184159, ; 76: lib_System.Net.NetworkInformation.dll.so => 115
	i64 u0x2a6507a5ffabdf28, ; 77: System.Diagnostics.TraceSource.dll => 104
	i64 u0x2ad156c8e1354139, ; 78: fi/Microsoft.Maui.Controls.resources => 7
	i64 u0x2af298f63581d886, ; 79: System.Text.RegularExpressions.dll => 137
	i64 u0x2af615542f04da50, ; 80: System.IdentityModel.Tokens.Jwt.dll => 63
	i64 u0x2afc1c4f898552ee, ; 81: lib_System.Formats.Asn1.dll.so => 106
	i64 u0x2b148910ed40fbf9, ; 82: zh-Hant/Microsoft.Maui.Controls.resources.dll => 33
	i64 u0x2c42baa2af5f9385, ; 83: lib_MimeMapping.dll.so => 54
	i64 u0x2c8bd14bb93a7d82, ; 84: lib-pl-Microsoft.Maui.Controls.resources.dll.so => 20
	i64 u0x2d169d318a968379, ; 85: System.Threading.dll => 140
	i64 u0x2d47774b7d993f59, ; 86: sv/Microsoft.Maui.Controls.resources.dll => 26
	i64 u0x2db915caf23548d2, ; 87: System.Text.Json.dll => 136
	i64 u0x2e6f1f226821322a, ; 88: el/Microsoft.Maui.Controls.resources.dll => 5
	i64 u0x2f2e98e1c89b1aff, ; 89: System.Xml.ReaderWriter => 142
	i64 u0x2f5911d9ba814e4e, ; 90: System.Diagnostics.Tracing => 105
	i64 u0x301c0086136fedbd, ; 91: lib_BirdMessenger.dll.so => 35
	i64 u0x309ee9eeec09a71e, ; 92: lib_Xamarin.AndroidX.Fragment.dll.so => 76
	i64 u0x309f2bedefa9a318, ; 93: Microsoft.IdentityModel.Abstractions => 44
	i64 u0x31195fef5d8fb552, ; 94: _Microsoft.Android.Resource.Designer.dll => 34
	i64 u0x32243413e774362a, ; 95: Xamarin.AndroidX.CardView.dll => 69
	i64 u0x329753a17a517811, ; 96: fr/Microsoft.Maui.Controls.resources => 8
	i64 u0x32aa989ff07a84ff, ; 97: lib_System.Xml.ReaderWriter.dll.so => 142
	i64 u0x32dbba5d256b19d3, ; 98: Supabase.Core => 56
	i64 u0x33a31443733849fe, ; 99: lib-es-Microsoft.Maui.Controls.resources.dll.so => 6
	i64 u0x341abc357fbb4ebf, ; 100: lib_System.Net.Sockets.dll.so => 120
	i64 u0x34dfd74fe2afcf37, ; 101: Microsoft.Maui => 51
	i64 u0x34e292762d9615df, ; 102: cs/Microsoft.Maui.Controls.resources.dll => 2
	i64 u0x3508234247f48404, ; 103: Microsoft.Maui.Controls => 49
	i64 u0x3549870798b4cd30, ; 104: lib_Xamarin.AndroidX.ViewPager2.dll.so => 90
	i64 u0x355282fc1c909694, ; 105: Microsoft.Extensions.Configuration => 36
	i64 u0x385c17636bb6fe6e, ; 106: Xamarin.AndroidX.CustomView.dll => 74
	i64 u0x38869c811d74050e, ; 107: System.Net.NameResolution.dll => 114
	i64 u0x393c226616977fdb, ; 108: lib_Xamarin.AndroidX.ViewPager.dll.so => 89
	i64 u0x395b3053dde89e41, ; 109: lib_System.Reactive.dll.so => 64
	i64 u0x395e37c3334cf82a, ; 110: lib-ca-Microsoft.Maui.Controls.resources.dll.so => 1
	i64 u0x39a87563fdb248a0, ; 111: System.Reactive.dll => 64
	i64 u0x3bea9ebe8c027c01, ; 112: lib_Microsoft.IdentityModel.Tokens.dll.so => 47
	i64 u0x3c7c495f58ac5ee9, ; 113: Xamarin.Kotlin.StdLib => 92
	i64 u0x3d9c2a242b040a50, ; 114: lib_Xamarin.AndroidX.Core.dll.so => 72
	i64 u0x4007fff231dcbc12, ; 115: lib_Supabase.Postgrest.dll.so => 59
	i64 u0x407a10bb4bf95829, ; 116: lib_Xamarin.AndroidX.Navigation.Common.dll.so => 82
	i64 u0x41cab042be111c34, ; 117: lib_Xamarin.AndroidX.AppCompat.AppCompatResources.dll.so => 68
	i64 u0x43375950ec7c1b6a, ; 118: netstandard.dll => 144
	i64 u0x434c4e1d9284cdae, ; 119: Mono.Android.dll => 148
	i64 u0x43950f84de7cc79a, ; 120: pl/Microsoft.Maui.Controls.resources.dll => 20
	i64 u0x4499fa3c8e494654, ; 121: lib_System.Runtime.Serialization.Primitives.dll.so => 131
	i64 u0x4515080865a951a5, ; 122: Xamarin.Kotlin.StdLib.dll => 92
	i64 u0x458d2df79ac57c1d, ; 123: lib_System.IdentityModel.Tokens.Jwt.dll.so => 63
	i64 u0x45c40276a42e283e, ; 124: System.Diagnostics.TraceSource => 104
	i64 u0x46a4213bc97fe5ae, ; 125: lib-ru-Microsoft.Maui.Controls.resources.dll.so => 24
	i64 u0x47daf4e1afbada10, ; 126: pt/Microsoft.Maui.Controls.resources => 22
	i64 u0x49e952f19a4e2022, ; 127: System.ObjectModel => 125
	i64 u0x4a5667b2462a664b, ; 128: lib_Xamarin.AndroidX.Navigation.UI.dll.so => 85
	i64 u0x4b7b6532ded934b7, ; 129: System.Text.Json => 136
	i64 u0x4cc5f15266470798, ; 130: lib_Xamarin.AndroidX.Loader.dll.so => 81
	i64 u0x4cf6f67dc77aacd2, ; 131: System.Net.NetworkInformation.dll => 115
	i64 u0x4d3183dd245425d4, ; 132: System.Net.WebSockets.Client.dll => 122
	i64 u0x4d479f968a05e504, ; 133: System.Linq.Expressions.dll => 110
	i64 u0x4d55a010ffc4faff, ; 134: System.Private.Xml => 127
	i64 u0x4d95fccc1f67c7ca, ; 135: System.Runtime.Loader.dll => 129
	i64 u0x4dcf44c3c9b076a2, ; 136: it/Microsoft.Maui.Controls.resources.dll => 14
	i64 u0x4dd9247f1d2c3235, ; 137: Xamarin.AndroidX.Loader.dll => 81
	i64 u0x4e32f00cb0937401, ; 138: Mono.Android.Runtime => 147
	i64 u0x4e8cf86fe3ecbfcd, ; 139: Supabase => 55
	i64 u0x4ebd0c4b82c5eefc, ; 140: lib_System.Threading.Channels.dll.so => 138
	i64 u0x4f0f420f6c43234c, ; 141: MimeMapping => 54
	i64 u0x4f21ee6ef9eb527e, ; 142: ca/Microsoft.Maui.Controls.resources => 1
	i64 u0x4ffd65baff757598, ; 143: Microsoft.IdentityModel.Tokens => 47
	i64 u0x5037f0be3c28c7a3, ; 144: lib_Microsoft.Maui.Controls.dll.so => 49
	i64 u0x5131bbe80989093f, ; 145: Xamarin.AndroidX.Lifecycle.ViewModel.Android.dll => 79
	i64 u0x526ce79eb8e90527, ; 146: lib_System.Net.Primitives.dll.so => 117
	i64 u0x529ffe06f39ab8db, ; 147: Xamarin.AndroidX.Core => 72
	i64 u0x52ff996554dbf352, ; 148: Microsoft.Maui.Graphics => 53
	i64 u0x535f7e40e8fef8af, ; 149: lib-sk-Microsoft.Maui.Controls.resources.dll.so => 25
	i64 u0x53a96d5c86c9e194, ; 150: System.Net.NetworkInformation => 115
	i64 u0x53c3014b9437e684, ; 151: lib-zh-HK-Microsoft.Maui.Controls.resources.dll.so => 31
	i64 u0x5435e6f049e9bc37, ; 152: System.Security.Claims.dll => 133
	i64 u0x54795225dd1587af, ; 153: lib_System.Runtime.dll.so => 132
	i64 u0x556e8b63b660ab8b, ; 154: Xamarin.AndroidX.Lifecycle.Common.Jvm.dll => 77
	i64 u0x5588627c9a108ec9, ; 155: System.Collections.Specialized => 98
	i64 u0x571c5cfbec5ae8e2, ; 156: System.Private.Uri => 126
	i64 u0x579a06fed6eec900, ; 157: System.Private.CoreLib.dll => 145
	i64 u0x57c542c14049b66d, ; 158: System.Diagnostics.DiagnosticSource => 62
	i64 u0x58601b2dda4a27b9, ; 159: lib-ja-Microsoft.Maui.Controls.resources.dll.so => 15
	i64 u0x58688d9af496b168, ; 160: Microsoft.Extensions.DependencyInjection.dll => 38
	i64 u0x5a89a886ae30258d, ; 161: lib_Xamarin.AndroidX.CoordinatorLayout.dll.so => 71
	i64 u0x5a8f6699f4a1caa9, ; 162: lib_System.Threading.dll.so => 140
	i64 u0x5ae9cd33b15841bf, ; 163: System.ComponentModel => 102
	i64 u0x5b5f0e240a06a2a2, ; 164: da/Microsoft.Maui.Controls.resources.dll => 3
	i64 u0x5c393624b8176517, ; 165: lib_Microsoft.Extensions.Logging.dll.so => 40
	i64 u0x5d0a4a29b02d9d3c, ; 166: System.Net.WebHeaderCollection.dll => 121
	i64 u0x5db0cbbd1028510e, ; 167: lib_System.Runtime.InteropServices.dll.so => 128
	i64 u0x5db30905d3e5013b, ; 168: Xamarin.AndroidX.Collection.Jvm.dll => 70
	i64 u0x5e467bc8f09ad026, ; 169: System.Collections.Specialized.dll => 98
	i64 u0x5ea92fdb19ec8c4c, ; 170: System.Text.Encodings.Web.dll => 135
	i64 u0x5eb8046dd40e9ac3, ; 171: System.ComponentModel.Primitives => 100
	i64 u0x5f36ccf5c6a57e24, ; 172: System.Xml.ReaderWriter.dll => 142
	i64 u0x5f9a2d823f664957, ; 173: lib-el-Microsoft.Maui.Controls.resources.dll.so => 5
	i64 u0x609f4b7b63d802d4, ; 174: lib_Microsoft.Extensions.DependencyInjection.dll.so => 38
	i64 u0x60cd4e33d7e60134, ; 175: Xamarin.KotlinX.Coroutines.Core.Jvm => 93
	i64 u0x60f62d786afcf130, ; 176: System.Memory => 112
	i64 u0x61be8d1299194243, ; 177: Microsoft.Maui.Controls.Xaml => 50
	i64 u0x61d2cba29557038f, ; 178: de/Microsoft.Maui.Controls.resources => 4
	i64 u0x61d88f399afb2f45, ; 179: lib_System.Runtime.Loader.dll.so => 129
	i64 u0x622eef6f9e59068d, ; 180: System.Private.CoreLib => 145
	i64 u0x6400f68068c1e9f1, ; 181: Xamarin.Google.Android.Material.dll => 91
	i64 u0x65ecac39144dd3cc, ; 182: Microsoft.Maui.Controls.dll => 49
	i64 u0x65ece51227bfa724, ; 183: lib_System.Runtime.Numerics.dll.so => 130
	i64 u0x6692e924eade1b29, ; 184: lib_System.Console.dll.so => 103
	i64 u0x66a4e5c6a3fb0bae, ; 185: lib_Xamarin.AndroidX.Lifecycle.ViewModel.Android.dll.so => 79
	i64 u0x66d13304ce1a3efa, ; 186: Xamarin.AndroidX.CursorAdapter => 73
	i64 u0x68558ec653afa616, ; 187: lib-da-Microsoft.Maui.Controls.resources.dll.so => 3
	i64 u0x68fbbbe2eb455198, ; 188: System.Formats.Asn1 => 106
	i64 u0x69063fc0ba8e6bdd, ; 189: he/Microsoft.Maui.Controls.resources.dll => 9
	i64 u0x6a4d7577b2317255, ; 190: System.Runtime.InteropServices.dll => 128
	i64 u0x6ace3b74b15ee4a4, ; 191: nb/Microsoft.Maui.Controls.resources => 18
	i64 u0x6ca323bb74a4c28a, ; 192: Supabase.Storage.dll => 61
	i64 u0x6d12bfaa99c72b1f, ; 193: lib_Microsoft.Maui.Graphics.dll.so => 53
	i64 u0x6d79993361e10ef2, ; 194: Microsoft.Extensions.Primitives => 43
	i64 u0x6d86d56b84c8eb71, ; 195: lib_Xamarin.AndroidX.CursorAdapter.dll.so => 73
	i64 u0x6d9bea6b3e895cf7, ; 196: Microsoft.Extensions.Primitives.dll => 43
	i64 u0x6e25a02c3833319a, ; 197: lib_Xamarin.AndroidX.Navigation.Fragment.dll.so => 83
	i64 u0x6f549bdbd19c7a4d, ; 198: Supabase.Gotrue => 58
	i64 u0x6fd2265da78b93a4, ; 199: lib_Microsoft.Maui.dll.so => 51
	i64 u0x6fdfc7de82c33008, ; 200: cs/Microsoft.Maui.Controls.resources => 2
	i64 u0x70e99f48c05cb921, ; 201: tr/Microsoft.Maui.Controls.resources.dll => 28
	i64 u0x70fd3deda22442d2, ; 202: lib-nb-Microsoft.Maui.Controls.resources.dll.so => 18
	i64 u0x71a495ea3761dde8, ; 203: lib-it-Microsoft.Maui.Controls.resources.dll.so => 14
	i64 u0x71ad672adbe48f35, ; 204: System.ComponentModel.Primitives.dll => 100
	i64 u0x72b1fb4109e08d7b, ; 205: lib-hr-Microsoft.Maui.Controls.resources.dll.so => 11
	i64 u0x73e4ce94e2eb6ffc, ; 206: lib_System.Memory.dll.so => 112
	i64 u0x755a91767330b3d4, ; 207: lib_Microsoft.Extensions.Configuration.dll.so => 36
	i64 u0x76012e7334db86e5, ; 208: lib_Xamarin.AndroidX.SavedState.dll.so => 87
	i64 u0x76ca07b878f44da0, ; 209: System.Runtime.Numerics.dll => 130
	i64 u0x780bc73597a503a9, ; 210: lib-ms-Microsoft.Maui.Controls.resources.dll.so => 17
	i64 u0x783606d1e53e7a1a, ; 211: th/Microsoft.Maui.Controls.resources.dll => 27
	i64 u0x78a45e51311409b6, ; 212: Xamarin.AndroidX.Fragment.dll => 76
	i64 u0x7adb8da2ac89b647, ; 213: fi/Microsoft.Maui.Controls.resources.dll => 7
	i64 u0x7b4927e421291c41, ; 214: Microsoft.IdentityModel.JsonWebTokens.dll => 45
	i64 u0x7bef86a4335c4870, ; 215: System.ComponentModel.TypeConverter => 101
	i64 u0x7c0820144cd34d6a, ; 216: sk/Microsoft.Maui.Controls.resources.dll => 25
	i64 u0x7c2a0bd1e0f988fc, ; 217: lib-de-Microsoft.Maui.Controls.resources.dll.so => 4
	i64 u0x7d649b75d580bb42, ; 218: ms/Microsoft.Maui.Controls.resources.dll => 17
	i64 u0x7d8ee2bdc8e3aad1, ; 219: System.Numerics.Vectors => 124
	i64 u0x7dfc3d6d9d8d7b70, ; 220: System.Collections => 99
	i64 u0x7e2e564fa2f76c65, ; 221: lib_System.Diagnostics.Tracing.dll.so => 105
	i64 u0x7e302e110e1e1346, ; 222: lib_System.Security.Claims.dll.so => 133
	i64 u0x7e946809d6008ef2, ; 223: lib_System.ObjectModel.dll.so => 125
	i64 u0x7ecc13347c8fd849, ; 224: lib_System.ComponentModel.dll.so => 102
	i64 u0x7f00ddd9b9ca5a13, ; 225: Xamarin.AndroidX.ViewPager.dll => 89
	i64 u0x7f9351cd44b1273f, ; 226: Microsoft.Extensions.Configuration.Abstractions => 37
	i64 u0x7fbd557c99b3ce6f, ; 227: lib_Xamarin.AndroidX.Lifecycle.LiveData.Core.dll.so => 78
	i64 u0x812c069d5cdecc17, ; 228: System.dll => 143
	i64 u0x81ab745f6c0f5ce6, ; 229: zh-Hant/Microsoft.Maui.Controls.resources => 33
	i64 u0x8277f2be6b5ce05f, ; 230: Xamarin.AndroidX.AppCompat => 67
	i64 u0x828f06563b30bc50, ; 231: lib_Xamarin.AndroidX.CardView.dll.so => 69
	i64 u0x82f6403342e12049, ; 232: uk/Microsoft.Maui.Controls.resources => 29
	i64 u0x83a2d9ad3c54f4f8, ; 233: MimeMapping.dll => 54
	i64 u0x83a7afd2c49adc86, ; 234: lib_Microsoft.IdentityModel.Abstractions.dll.so => 44
	i64 u0x83c14ba66c8e2b8c, ; 235: zh-Hans/Microsoft.Maui.Controls.resources => 32
	i64 u0x86a909228dc7657b, ; 236: lib-zh-Hant-Microsoft.Maui.Controls.resources.dll.so => 33
	i64 u0x86b3e00c36b84509, ; 237: Microsoft.Extensions.Configuration.dll => 36
	i64 u0x87c69b87d9283884, ; 238: lib_System.Threading.Thread.dll.so => 139
	i64 u0x87f6569b25707834, ; 239: System.IO.Compression.Brotli.dll => 107
	i64 u0x8842b3a5d2d3fb36, ; 240: Microsoft.Maui.Essentials => 52
	i64 u0x88bda98e0cffb7a9, ; 241: lib_Xamarin.KotlinX.Coroutines.Core.Jvm.dll.so => 93
	i64 u0x8930322c7bd8f768, ; 242: netstandard => 144
	i64 u0x897a606c9e39c75f, ; 243: lib_System.ComponentModel.Primitives.dll.so => 100
	i64 u0x8ad229ea26432ee2, ; 244: Xamarin.AndroidX.Loader => 81
	i64 u0x8b4ff5d0fdd5faa1, ; 245: lib_System.Diagnostics.DiagnosticSource.dll.so => 62
	i64 u0x8b9ceca7acae3451, ; 246: lib-he-Microsoft.Maui.Controls.resources.dll.so => 9
	i64 u0x8d0f420977c2c1c7, ; 247: Xamarin.AndroidX.CursorAdapter.dll => 73
	i64 u0x8d7b8ab4b3310ead, ; 248: System.Threading => 140
	i64 u0x8da188285aadfe8e, ; 249: System.Collections.Concurrent => 96
	i64 u0x8ed807bfe9858dfc, ; 250: Xamarin.AndroidX.Navigation.Common => 82
	i64 u0x8ee08b8194a30f48, ; 251: lib-hi-Microsoft.Maui.Controls.resources.dll.so => 10
	i64 u0x8ef7601039857a44, ; 252: lib-ro-Microsoft.Maui.Controls.resources.dll.so => 23
	i64 u0x8f32c6f611f6ffab, ; 253: pt/Microsoft.Maui.Controls.resources.dll => 22
	i64 u0x8f8829d21c8985a4, ; 254: lib-pt-BR-Microsoft.Maui.Controls.resources.dll.so => 21
	i64 u0x90263f8448b8f572, ; 255: lib_System.Diagnostics.TraceSource.dll.so => 104
	i64 u0x903101b46fb73a04, ; 256: _Microsoft.Android.Resource.Designer => 34
	i64 u0x90393bd4865292f3, ; 257: lib_System.IO.Compression.dll.so => 108
	i64 u0x90634f86c5ebe2b5, ; 258: Xamarin.AndroidX.Lifecycle.ViewModel.Android => 79
	i64 u0x907b636704ad79ef, ; 259: lib_Microsoft.Maui.Controls.Xaml.dll.so => 50
	i64 u0x91418dc638b29e68, ; 260: lib_Xamarin.AndroidX.CustomView.dll.so => 74
	i64 u0x9157bd523cd7ed36, ; 261: lib_System.Text.Json.dll.so => 136
	i64 u0x91a74f07b30d37e2, ; 262: System.Linq.dll => 111
	i64 u0x91fa41a87223399f, ; 263: ca/Microsoft.Maui.Controls.resources.dll => 1
	i64 u0x925923af91e359c8, ; 264: lib_gitosfiesta.dll.so => 95
	i64 u0x93cfa73ab28d6e35, ; 265: ms/Microsoft.Maui.Controls.resources => 17
	i64 u0x944077d8ca3c6580, ; 266: System.IO.Compression.dll => 108
	i64 u0x948d746a7702861f, ; 267: Microsoft.IdentityModel.Logging.dll => 46
	i64 u0x9564283c37ed59a9, ; 268: lib_Microsoft.IdentityModel.Logging.dll.so => 46
	i64 u0x967fc325e09bfa8c, ; 269: es/Microsoft.Maui.Controls.resources => 6
	i64 u0x9732d8dbddea3d9a, ; 270: id/Microsoft.Maui.Controls.resources => 13
	i64 u0x978be80e5210d31b, ; 271: Microsoft.Maui.Graphics.dll => 53
	i64 u0x97b8c771ea3e4220, ; 272: System.ComponentModel.dll => 102
	i64 u0x97e144c9d3c6976e, ; 273: System.Collections.Concurrent.dll => 96
	i64 u0x991d510397f92d9d, ; 274: System.Linq.Expressions => 110
	i64 u0x99a00ca5270c6878, ; 275: Xamarin.AndroidX.Navigation.Runtime => 84
	i64 u0x99cdc6d1f2d3a72f, ; 276: ko/Microsoft.Maui.Controls.resources.dll => 16
	i64 u0x9a816d9654deff7c, ; 277: Microsoft.IO.RecyclableMemoryStream => 48
	i64 u0x9c36a0f95393e81c, ; 278: Supabase.Postgrest.dll => 59
	i64 u0x9d5dbcf5a48583fe, ; 279: lib_Xamarin.AndroidX.Activity.dll.so => 66
	i64 u0x9d74dee1a7725f34, ; 280: Microsoft.Extensions.Configuration.Abstractions.dll => 37
	i64 u0x9e4534b6adaf6e84, ; 281: nl/Microsoft.Maui.Controls.resources => 19
	i64 u0x9eaf1efdf6f7267e, ; 282: Xamarin.AndroidX.Navigation.Common.dll => 82
	i64 u0x9ef542cf1f78c506, ; 283: Xamarin.AndroidX.Lifecycle.LiveData.Core => 78
	i64 u0xa0d8259f4cc284ec, ; 284: lib_System.Security.Cryptography.dll.so => 134
	i64 u0xa12a8378511c135a, ; 285: BirdMessenger.dll => 35
	i64 u0xa135be3d6497d3d3, ; 286: Supabase.Core.dll => 56
	i64 u0xa1440773ee9d341e, ; 287: Xamarin.Google.Android.Material => 91
	i64 u0xa1b9d7c27f47219f, ; 288: Xamarin.AndroidX.Navigation.UI.dll => 85
	i64 u0xa2572680829d2c7c, ; 289: System.IO.Pipelines.dll => 109
	i64 u0xa46aa1eaa214539b, ; 290: ko/Microsoft.Maui.Controls.resources => 16
	i64 u0xa5e599d1e0524750, ; 291: System.Numerics.Vectors.dll => 124
	i64 u0xa5f1ba49b85dd355, ; 292: System.Security.Cryptography.dll => 134
	i64 u0xa67dbee13e1df9ca, ; 293: Xamarin.AndroidX.SavedState.dll => 87
	i64 u0xa68a420042bb9b1f, ; 294: Xamarin.AndroidX.DrawerLayout.dll => 75
	i64 u0xa78ce3745383236a, ; 295: Xamarin.AndroidX.Lifecycle.Common.Jvm => 77
	i64 u0xa7c31b56b4dc7b33, ; 296: hu/Microsoft.Maui.Controls.resources => 12
	i64 u0xa8e6320dd07580ef, ; 297: lib_Microsoft.IdentityModel.JsonWebTokens.dll.so => 45
	i64 u0xa9240518980b3aab, ; 298: BirdMessenger => 35
	i64 u0xaa2219c8e3449ff5, ; 299: Microsoft.Extensions.Logging.Abstractions => 41
	i64 u0xaa443ac34067eeef, ; 300: System.Private.Xml.dll => 127
	i64 u0xaa52de307ef5d1dd, ; 301: System.Net.Http => 113
	i64 u0xaaaf86367285a918, ; 302: Microsoft.Extensions.DependencyInjection.Abstractions.dll => 39
	i64 u0xaaf84bb3f052a265, ; 303: el/Microsoft.Maui.Controls.resources => 5
	i64 u0xab9c1b2687d86b0b, ; 304: lib_System.Linq.Expressions.dll.so => 110
	i64 u0xac2af3fa195a15ce, ; 305: System.Runtime.Numerics => 130
	i64 u0xac5376a2a538dc10, ; 306: Xamarin.AndroidX.Lifecycle.LiveData.Core.dll => 78
	i64 u0xacd46e002c3ccb97, ; 307: ro/Microsoft.Maui.Controls.resources => 23
	i64 u0xacf42eea7ef9cd12, ; 308: System.Threading.Channels => 138
	i64 u0xad12c014a329db4c, ; 309: gitosfiesta => 95
	i64 u0xad89c07347f1bad6, ; 310: nl/Microsoft.Maui.Controls.resources.dll => 19
	i64 u0xadbb53caf78a79d2, ; 311: System.Web.HttpUtility => 141
	i64 u0xadc90ab061a9e6e4, ; 312: System.ComponentModel.TypeConverter.dll => 101
	i64 u0xadf511667bef3595, ; 313: System.Net.Security => 119
	i64 u0xae282bcd03739de7, ; 314: Java.Interop => 146
	i64 u0xae53579c90db1107, ; 315: System.ObjectModel.dll => 125
	i64 u0xafe29f45095518e7, ; 316: lib_Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll.so => 80
	i64 u0xb05b6f0a6cc8ddbb, ; 317: lib_Microsoft.IO.RecyclableMemoryStream.dll.so => 48
	i64 u0xb05cc42cd94c6d9d, ; 318: lib-sv-Microsoft.Maui.Controls.resources.dll.so => 26
	i64 u0xb0bb43dc52ea59f9, ; 319: System.Diagnostics.Tracing.dll => 105
	i64 u0xb220631954820169, ; 320: System.Text.RegularExpressions => 137
	i64 u0xb2a3f67f3bf29fce, ; 321: da/Microsoft.Maui.Controls.resources => 3
	i64 u0xb3f0a0fcda8d3ebc, ; 322: Xamarin.AndroidX.CardView => 69
	i64 u0xb46be1aa6d4fff93, ; 323: hi/Microsoft.Maui.Controls.resources => 10
	i64 u0xb477491be13109d8, ; 324: ar/Microsoft.Maui.Controls.resources => 0
	i64 u0xb4bd7015ecee9d86, ; 325: System.IO.Pipelines => 109
	i64 u0xb5c7fcdafbc67ee4, ; 326: Microsoft.Extensions.Logging.Abstractions.dll => 41
	i64 u0xb7b7753d1f319409, ; 327: sv/Microsoft.Maui.Controls.resources => 26
	i64 u0xb81a2c6e0aee50fe, ; 328: lib_System.Private.CoreLib.dll.so => 145
	i64 u0xb9f64d3b230def68, ; 329: lib-pt-Microsoft.Maui.Controls.resources.dll.so => 22
	i64 u0xb9fc3c8a556e3691, ; 330: ja/Microsoft.Maui.Controls.resources => 15
	i64 u0xba48785529705af9, ; 331: System.Collections.dll => 99
	i64 u0xbb54fd4c9d1101e1, ; 332: lib_Supabase.dll.so => 55
	i64 u0xbb65706fde942ce3, ; 333: System.Net.Sockets => 120
	i64 u0xbd0e2c0d55246576, ; 334: System.Net.Http.dll => 113
	i64 u0xbd437a2cdb333d0d, ; 335: Xamarin.AndroidX.ViewPager2 => 90
	i64 u0xbee38d4a88835966, ; 336: Xamarin.AndroidX.AppCompat.AppCompatResources => 68
	i64 u0xbf02c92392c99ce0, ; 337: Websocket.Client => 65
	i64 u0xc040a4ab55817f58, ; 338: ar/Microsoft.Maui.Controls.resources.dll => 0
	i64 u0xc0d928351ab5ca77, ; 339: System.Console.dll => 103
	i64 u0xc12b8b3afa48329c, ; 340: lib_System.Linq.dll.so => 111
	i64 u0xc1ff9ae3cdb6e1e6, ; 341: Xamarin.AndroidX.Activity.dll => 66
	i64 u0xc278de356ad8a9e3, ; 342: Microsoft.IdentityModel.Logging => 46
	i64 u0xc28c50f32f81cc73, ; 343: ja/Microsoft.Maui.Controls.resources.dll => 15
	i64 u0xc2bcfec99f69365e, ; 344: Xamarin.AndroidX.ViewPager2.dll => 90
	i64 u0xc421b61fd853169d, ; 345: lib_System.Net.WebSockets.Client.dll.so => 122
	i64 u0xc4d3858ed4d08512, ; 346: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll => 80
	i64 u0xc50fded0ded1418c, ; 347: lib_System.ComponentModel.TypeConverter.dll.so => 101
	i64 u0xc519125d6bc8fb11, ; 348: lib_System.Net.Requests.dll.so => 118
	i64 u0xc5293b19e4dc230e, ; 349: Xamarin.AndroidX.Navigation.Fragment => 83
	i64 u0xc5325b2fcb37446f, ; 350: lib_System.Private.Xml.dll.so => 127
	i64 u0xc5a0f4b95a699af7, ; 351: lib_System.Private.Uri.dll.so => 126
	i64 u0xc7ce851898a4548e, ; 352: lib_System.Web.HttpUtility.dll.so => 141
	i64 u0xc858a28d9ee5a6c5, ; 353: lib_System.Collections.Specialized.dll.so => 98
	i64 u0xca3a723e7342c5b6, ; 354: lib-tr-Microsoft.Maui.Controls.resources.dll.so => 28
	i64 u0xcab3493c70141c2d, ; 355: pl/Microsoft.Maui.Controls.resources => 20
	i64 u0xcacfddc9f7c6de76, ; 356: ro/Microsoft.Maui.Controls.resources.dll => 23
	i64 u0xcb76efab0f56f81a, ; 357: System.Reactive => 64
	i64 u0xcbd4fdd9cef4a294, ; 358: lib__Microsoft.Android.Resource.Designer.dll.so => 34
	i64 u0xcc2876b32ef2794c, ; 359: lib_System.Text.RegularExpressions.dll.so => 137
	i64 u0xcc5c3bb714c4561e, ; 360: Xamarin.KotlinX.Coroutines.Core.Jvm.dll => 93
	i64 u0xcc76886e09b88260, ; 361: Xamarin.KotlinX.Serialization.Core.Jvm.dll => 94
	i64 u0xccf25c4b634ccd3a, ; 362: zh-Hans/Microsoft.Maui.Controls.resources.dll => 32
	i64 u0xcd10a42808629144, ; 363: System.Net.Requests => 118
	i64 u0xcdd0c48b6937b21c, ; 364: Xamarin.AndroidX.SwipeRefreshLayout => 88
	i64 u0xcf23d8093f3ceadf, ; 365: System.Diagnostics.DiagnosticSource.dll => 62
	i64 u0xd1194e1d8a8de83c, ; 366: lib_Xamarin.AndroidX.Lifecycle.Common.Jvm.dll.so => 77
	i64 u0xd333d0af9e423810, ; 367: System.Runtime.InteropServices => 128
	i64 u0xd3426d966bb704f5, ; 368: Xamarin.AndroidX.AppCompat.AppCompatResources.dll => 68
	i64 u0xd3651b6fc3125825, ; 369: System.Private.Uri.dll => 126
	i64 u0xd373685349b1fe8b, ; 370: Microsoft.Extensions.Logging.dll => 40
	i64 u0xd3e4c8d6a2d5d470, ; 371: it/Microsoft.Maui.Controls.resources => 14
	i64 u0xd4645626dffec99d, ; 372: lib_Microsoft.Extensions.DependencyInjection.Abstractions.dll.so => 39
	i64 u0xd5507e11a2b2839f, ; 373: Xamarin.AndroidX.Lifecycle.ViewModelSavedState => 80
	i64 u0xd6694f8359737e4e, ; 374: Xamarin.AndroidX.SavedState => 87
	i64 u0xd6d21782156bc35b, ; 375: Xamarin.AndroidX.SwipeRefreshLayout.dll => 88
	i64 u0xd72329819cbbbc44, ; 376: lib_Microsoft.Extensions.Configuration.Abstractions.dll.so => 37
	i64 u0xd7b3764ada9d341d, ; 377: lib_Microsoft.Extensions.Logging.Abstractions.dll.so => 41
	i64 u0xda1dfa4c534a9251, ; 378: Microsoft.Extensions.DependencyInjection => 38
	i64 u0xdad05a11827959a3, ; 379: System.Collections.NonGeneric.dll => 97
	i64 u0xdb5383ab5865c007, ; 380: lib-vi-Microsoft.Maui.Controls.resources.dll.so => 30
	i64 u0xdbeda89f832aa805, ; 381: vi/Microsoft.Maui.Controls.resources.dll => 30
	i64 u0xdbf9607a441b4505, ; 382: System.Linq => 111
	i64 u0xdce2c53525640bf3, ; 383: Microsoft.Extensions.Logging => 40
	i64 u0xdceda8d644ac18a6, ; 384: Supabase.dll => 55
	i64 u0xdd2b722d78ef5f43, ; 385: System.Runtime.dll => 132
	i64 u0xdd67031857c72f96, ; 386: lib_System.Text.Encodings.Web.dll.so => 135
	i64 u0xdde30e6b77aa6f6c, ; 387: lib-zh-Hans-Microsoft.Maui.Controls.resources.dll.so => 32
	i64 u0xde1223bb049720d1, ; 388: Supabase.Realtime => 60
	i64 u0xde8769ebda7d8647, ; 389: hr/Microsoft.Maui.Controls.resources.dll => 11
	i64 u0xdf25d9f36d8dc576, ; 390: lib_Supabase.Functions.dll.so => 57
	i64 u0xdfa254ebb4346068, ; 391: System.Net.Ping => 116
	i64 u0xe0142572c095a480, ; 392: Xamarin.AndroidX.AppCompat.dll => 67
	i64 u0xe02f89350ec78051, ; 393: Xamarin.AndroidX.CoordinatorLayout.dll => 71
	i64 u0xe192a588d4410686, ; 394: lib_System.IO.Pipelines.dll.so => 109
	i64 u0xe1a08bd3fa539e0d, ; 395: System.Runtime.Loader => 129
	i64 u0xe1ecfdb7fff86067, ; 396: System.Net.Security.dll => 119
	i64 u0xe2420585aeceb728, ; 397: System.Net.Requests.dll => 118
	i64 u0xe29b73bc11392966, ; 398: lib-id-Microsoft.Maui.Controls.resources.dll.so => 13
	i64 u0xe3811d68d4fe8463, ; 399: pt-BR/Microsoft.Maui.Controls.resources.dll => 21
	i64 u0xe494f7ced4ecd10a, ; 400: hu/Microsoft.Maui.Controls.resources.dll => 12
	i64 u0xe4a9b1e40d1e8917, ; 401: lib-fi-Microsoft.Maui.Controls.resources.dll.so => 7
	i64 u0xe4f74a0b5bf9703f, ; 402: System.Runtime.Serialization.Primitives => 131
	i64 u0xe5434e8a119ceb69, ; 403: lib_Mono.Android.dll.so => 148
	i64 u0xedc4817167106c23, ; 404: System.Net.Sockets.dll => 120
	i64 u0xedc632067fb20ff3, ; 405: System.Memory.dll => 112
	i64 u0xedc8e4ca71a02a8b, ; 406: Xamarin.AndroidX.Navigation.Runtime.dll => 84
	i64 u0xeeb7ebb80150501b, ; 407: lib_Xamarin.AndroidX.Collection.Jvm.dll.so => 70
	i64 u0xef72742e1bcca27a, ; 408: Microsoft.Maui.Essentials.dll => 52
	i64 u0xefec0b7fdc57ec42, ; 409: Xamarin.AndroidX.Activity => 66
	i64 u0xf00c29406ea45e19, ; 410: es/Microsoft.Maui.Controls.resources.dll => 6
	i64 u0xf09e47b6ae914f6e, ; 411: System.Net.NameResolution => 114
	i64 u0xf0de2537ee19c6ca, ; 412: lib_System.Net.WebHeaderCollection.dll.so => 121
	i64 u0xf11b621fc87b983f, ; 413: Microsoft.Maui.Controls.Xaml.dll => 50
	i64 u0xf1c4b4005493d871, ; 414: System.Formats.Asn1.dll => 106
	i64 u0xf238bd79489d3a96, ; 415: lib-nl-Microsoft.Maui.Controls.resources.dll.so => 19
	i64 u0xf37221fda4ef8830, ; 416: lib_Xamarin.Google.Android.Material.dll.so => 91
	i64 u0xf3ddfe05336abf29, ; 417: System => 143
	i64 u0xf47e294cbe68c2b0, ; 418: lib_Websocket.Client.dll.so => 65
	i64 u0xf4c1dd70a5496a17, ; 419: System.IO.Compression => 108
	i64 u0xf5fc7602fe27b333, ; 420: System.Net.WebHeaderCollection => 121
	i64 u0xf6077741019d7428, ; 421: Xamarin.AndroidX.CoordinatorLayout => 71
	i64 u0xf61ade9836ad4692, ; 422: Microsoft.IdentityModel.Tokens.dll => 47
	i64 u0xf6c0e7d55a7a4e4f, ; 423: Microsoft.IdentityModel.JsonWebTokens => 45
	i64 u0xf77b20923f07c667, ; 424: de/Microsoft.Maui.Controls.resources.dll => 4
	i64 u0xf7d5da3db84fa88c, ; 425: Supabase.Postgrest => 59
	i64 u0xf7e2cac4c45067b3, ; 426: lib_System.Numerics.Vectors.dll.so => 124
	i64 u0xf7e74930e0e3d214, ; 427: zh-HK/Microsoft.Maui.Controls.resources.dll => 31
	i64 u0xf84773b5c81e3cef, ; 428: lib-uk-Microsoft.Maui.Controls.resources.dll.so => 29
	i64 u0xf8e045dc345b2ea3, ; 429: lib_Xamarin.AndroidX.RecyclerView.dll.so => 86
	i64 u0xf915dc29808193a1, ; 430: System.Web.HttpUtility.dll => 141
	i64 u0xf96c777a2a0686f4, ; 431: hi/Microsoft.Maui.Controls.resources.dll => 10
	i64 u0xf9eec5bb3a6aedc6, ; 432: Microsoft.Extensions.Options => 42
	i64 u0xfa3f278f288b0e84, ; 433: lib_System.Net.Security.dll.so => 119
	i64 u0xfa5ed7226d978949, ; 434: lib-ar-Microsoft.Maui.Controls.resources.dll.so => 0
	i64 u0xfa645d91e9fc4cba, ; 435: System.Threading.Thread => 139
	i64 u0xfb06dd2338e6f7c4, ; 436: System.Net.Ping.dll => 116
	i64 u0xfb35173928a89083, ; 437: Supabase.Functions.dll => 57
	i64 u0xfb7a682b00f50271, ; 438: lib_Supabase.Storage.dll.so => 61
	i64 u0xfbba65887a38c94f, ; 439: lib_Supabase.Core.dll.so => 56
	i64 u0xfbf0a31c9fc34bc4, ; 440: lib_System.Net.Http.dll.so => 113
	i64 u0xfc719aec26adf9d9, ; 441: Xamarin.AndroidX.Navigation.Fragment.dll => 83
	i64 u0xfd22f00870e40ae0, ; 442: lib_Xamarin.AndroidX.DrawerLayout.dll.so => 75
	i64 u0xfd583f7657b6a1cb, ; 443: Xamarin.AndroidX.Fragment => 76
	i64 u0xfda36abccf05cf5c, ; 444: System.Net.WebSockets.Client => 122
	i64 u0xfeae9952cf03b8cb, ; 445: tr/Microsoft.Maui.Controls.resources => 28
	i64 u0xffa1fe933cabf8e4 ; 446: Websocket.Client.dll => 65
], align 8

@assembly_image_cache_indices = dso_local local_unnamed_addr constant [447 x i32] [
	i32 88, i32 84, i32 58, i32 147, i32 67, i32 48, i32 24, i32 2,
	i32 30, i32 44, i32 117, i32 86, i32 99, i32 51, i32 31, i32 70,
	i32 24, i32 97, i32 75, i32 42, i32 97, i32 134, i32 138, i32 25,
	i32 94, i32 89, i32 21, i32 148, i32 52, i32 57, i32 114, i32 61,
	i32 60, i32 74, i32 107, i32 123, i32 86, i32 8, i32 146, i32 9,
	i32 39, i32 123, i32 144, i32 12, i32 135, i32 94, i32 18, i32 133,
	i32 96, i32 143, i32 27, i32 116, i32 147, i32 85, i32 58, i32 16,
	i32 42, i32 95, i32 107, i32 60, i32 132, i32 27, i32 139, i32 103,
	i32 72, i32 131, i32 8, i32 92, i32 43, i32 63, i32 13, i32 11,
	i32 123, i32 146, i32 117, i32 29, i32 115, i32 104, i32 7, i32 137,
	i32 63, i32 106, i32 33, i32 54, i32 20, i32 140, i32 26, i32 136,
	i32 5, i32 142, i32 105, i32 35, i32 76, i32 44, i32 34, i32 69,
	i32 8, i32 142, i32 56, i32 6, i32 120, i32 51, i32 2, i32 49,
	i32 90, i32 36, i32 74, i32 114, i32 89, i32 64, i32 1, i32 64,
	i32 47, i32 92, i32 72, i32 59, i32 82, i32 68, i32 144, i32 148,
	i32 20, i32 131, i32 92, i32 63, i32 104, i32 24, i32 22, i32 125,
	i32 85, i32 136, i32 81, i32 115, i32 122, i32 110, i32 127, i32 129,
	i32 14, i32 81, i32 147, i32 55, i32 138, i32 54, i32 1, i32 47,
	i32 49, i32 79, i32 117, i32 72, i32 53, i32 25, i32 115, i32 31,
	i32 133, i32 132, i32 77, i32 98, i32 126, i32 145, i32 62, i32 15,
	i32 38, i32 71, i32 140, i32 102, i32 3, i32 40, i32 121, i32 128,
	i32 70, i32 98, i32 135, i32 100, i32 142, i32 5, i32 38, i32 93,
	i32 112, i32 50, i32 4, i32 129, i32 145, i32 91, i32 49, i32 130,
	i32 103, i32 79, i32 73, i32 3, i32 106, i32 9, i32 128, i32 18,
	i32 61, i32 53, i32 43, i32 73, i32 43, i32 83, i32 58, i32 51,
	i32 2, i32 28, i32 18, i32 14, i32 100, i32 11, i32 112, i32 36,
	i32 87, i32 130, i32 17, i32 27, i32 76, i32 7, i32 45, i32 101,
	i32 25, i32 4, i32 17, i32 124, i32 99, i32 105, i32 133, i32 125,
	i32 102, i32 89, i32 37, i32 78, i32 143, i32 33, i32 67, i32 69,
	i32 29, i32 54, i32 44, i32 32, i32 33, i32 36, i32 139, i32 107,
	i32 52, i32 93, i32 144, i32 100, i32 81, i32 62, i32 9, i32 73,
	i32 140, i32 96, i32 82, i32 10, i32 23, i32 22, i32 21, i32 104,
	i32 34, i32 108, i32 79, i32 50, i32 74, i32 136, i32 111, i32 1,
	i32 95, i32 17, i32 108, i32 46, i32 46, i32 6, i32 13, i32 53,
	i32 102, i32 96, i32 110, i32 84, i32 16, i32 48, i32 59, i32 66,
	i32 37, i32 19, i32 82, i32 78, i32 134, i32 35, i32 56, i32 91,
	i32 85, i32 109, i32 16, i32 124, i32 134, i32 87, i32 75, i32 77,
	i32 12, i32 45, i32 35, i32 41, i32 127, i32 113, i32 39, i32 5,
	i32 110, i32 130, i32 78, i32 23, i32 138, i32 95, i32 19, i32 141,
	i32 101, i32 119, i32 146, i32 125, i32 80, i32 48, i32 26, i32 105,
	i32 137, i32 3, i32 69, i32 10, i32 0, i32 109, i32 41, i32 26,
	i32 145, i32 22, i32 15, i32 99, i32 55, i32 120, i32 113, i32 90,
	i32 68, i32 65, i32 0, i32 103, i32 111, i32 66, i32 46, i32 15,
	i32 90, i32 122, i32 80, i32 101, i32 118, i32 83, i32 127, i32 126,
	i32 141, i32 98, i32 28, i32 20, i32 23, i32 64, i32 34, i32 137,
	i32 93, i32 94, i32 32, i32 118, i32 88, i32 62, i32 77, i32 128,
	i32 68, i32 126, i32 40, i32 14, i32 39, i32 80, i32 87, i32 88,
	i32 37, i32 41, i32 38, i32 97, i32 30, i32 30, i32 111, i32 40,
	i32 55, i32 132, i32 135, i32 32, i32 60, i32 11, i32 57, i32 116,
	i32 67, i32 71, i32 109, i32 129, i32 119, i32 118, i32 13, i32 21,
	i32 12, i32 7, i32 131, i32 148, i32 120, i32 112, i32 84, i32 70,
	i32 52, i32 66, i32 6, i32 114, i32 121, i32 50, i32 106, i32 19,
	i32 91, i32 143, i32 65, i32 108, i32 121, i32 71, i32 47, i32 45,
	i32 4, i32 59, i32 124, i32 31, i32 29, i32 86, i32 141, i32 10,
	i32 42, i32 119, i32 0, i32 139, i32 116, i32 57, i32 61, i32 56,
	i32 113, i32 83, i32 75, i32 76, i32 122, i32 28, i32 65
], align 4

@marshal_methods_number_of_classes = dso_local local_unnamed_addr constant i32 0, align 4

@marshal_methods_class_cache = dso_local local_unnamed_addr global [0 x %struct.MarshalMethodsManagedClass] zeroinitializer, align 8

; Names of classes in which marshal methods reside
@mm_class_names = dso_local local_unnamed_addr constant [0 x ptr] zeroinitializer, align 8

@mm_method_names = dso_local local_unnamed_addr constant [1 x %struct.MarshalMethodName] [
	%struct.MarshalMethodName {
		i64 u0x0000000000000000, ; name: 
		ptr @.MarshalMethodName.0_name; char* name
	} ; 0
], align 8

; get_function_pointer (uint32_t mono_image_index, uint32_t class_index, uint32_t method_token, void*& target_ptr)
@get_function_pointer = internal dso_local unnamed_addr global ptr null, align 8

; Functions

; Function attributes: memory(write, argmem: none, inaccessiblemem: none) "min-legal-vector-width"="0" mustprogress "no-trapping-math"="true" nofree norecurse nosync nounwind "stack-protector-buffer-size"="8" uwtable willreturn
define void @xamarin_app_init(ptr nocapture noundef readnone %env, ptr noundef %fn) local_unnamed_addr #0
{
	%fnIsNull = icmp eq ptr %fn, null
	br i1 %fnIsNull, label %1, label %2

1: ; preds = %0
	%putsResult = call noundef i32 @puts(ptr @.str.0)
	call void @abort()
	unreachable 

2: ; preds = %1, %0
	store ptr %fn, ptr @get_function_pointer, align 8, !tbaa !3
	ret void
}

; Strings
@.str.0 = private unnamed_addr constant [40 x i8] c"get_function_pointer MUST be specified\0A\00", align 1

;MarshalMethodName
@.MarshalMethodName.0_name = private unnamed_addr constant [1 x i8] c"\00", align 1

; External functions

; Function attributes: "no-trapping-math"="true" noreturn nounwind "stack-protector-buffer-size"="8"
declare void @abort() local_unnamed_addr #2

; Function attributes: nofree nounwind
declare noundef i32 @puts(ptr noundef) local_unnamed_addr #1
attributes #0 = { memory(write, argmem: none, inaccessiblemem: none) "min-legal-vector-width"="0" mustprogress "no-trapping-math"="true" nofree norecurse nosync nounwind "stack-protector-buffer-size"="8" "target-cpu"="generic" "target-features"="+fix-cortex-a53-835769,+neon,+outline-atomics,+v8a" uwtable willreturn }
attributes #1 = { nofree nounwind }
attributes #2 = { "no-trapping-math"="true" noreturn nounwind "stack-protector-buffer-size"="8" "target-cpu"="generic" "target-features"="+fix-cortex-a53-835769,+neon,+outline-atomics,+v8a" }

; Metadata
!llvm.module.flags = !{!0, !1, !7, !8, !9, !10}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!llvm.ident = !{!2}
!2 = !{!".NET for Android remotes/origin/release/9.0.1xx @ 1dcfb6f8779c33b6f768c996495cb90ecd729329"}
!3 = !{!4, !4, i64 0}
!4 = !{!"any pointer", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
!7 = !{i32 1, !"branch-target-enforcement", i32 0}
!8 = !{i32 1, !"sign-return-address", i32 0}
!9 = !{i32 1, !"sign-return-address-all", i32 0}
!10 = !{i32 1, !"sign-return-address-with-bkey", i32 0}
