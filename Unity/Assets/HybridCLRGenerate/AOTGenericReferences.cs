using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"ETClient.Core.dll",
		"Luban.Runtime.dll",
		"Nino.Core.dll",
		"System.Core.dll",
		"System.Runtime.CompilerServices.Unsafe.dll",
		"System.dll",
		"UnityEngine.CoreModule.dll",
		"YooAsset.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// ET.AEvent<object,ET.AfterCreateClientScene>
	// ET.AEvent<object,ET.AfterCreateCurrentScene>
	// ET.AEvent<object,ET.AfterUnitCreate>
	// ET.AEvent<object,ET.AppStartInitFinish>
	// ET.AEvent<object,ET.ChangePosition>
	// ET.AEvent<object,ET.ChangeRotation>
	// ET.AEvent<object,ET.EnterMapFinish>
	// ET.AEvent<object,ET.EntryEvent1>
	// ET.AEvent<object,ET.EntryEvent3>
	// ET.AEvent<object,ET.LoginFinish>
	// ET.AEvent<object,ET.MoveStart>
	// ET.AEvent<object,ET.MoveStop>
	// ET.AEvent<object,ET.NumbericChange>
	// ET.AEvent<object,ET.SceneChangeFinish>
	// ET.AEvent<object,ET.SceneChangeStart>
	// ET.AInvokeHandler<ET.FiberInit,object>
	// ET.AInvokeHandler<ET.MailBoxInvoker>
	// ET.AInvokeHandler<ET.NetComponentOnRead>
	// ET.AInvokeHandler<ET.TimerCallback>
	// ET.AwakeSystem<object,int>
	// ET.AwakeSystem<object,long,long,int>
	// ET.AwakeSystem<object,long>
	// ET.AwakeSystem<object,object>
	// ET.AwakeSystem<object>
	// ET.DestroySystem<object>
	// ET.ETAsyncTaskMethodBuilder<ET.Wait_CreateMyUnit>
	// ET.ETAsyncTaskMethodBuilder<ET.Wait_SceneChangeFinish>
	// ET.ETAsyncTaskMethodBuilder<ET.Wait_UnitStop>
	// ET.ETAsyncTaskMethodBuilder<System.ValueTuple<uint,object>>
	// ET.ETAsyncTaskMethodBuilder<byte>
	// ET.ETAsyncTaskMethodBuilder<int>
	// ET.ETAsyncTaskMethodBuilder<object>
	// ET.ETAsyncTaskMethodBuilder<uint>
	// ET.ETTask<ET.Wait_CreateMyUnit>
	// ET.ETTask<ET.Wait_SceneChangeFinish>
	// ET.ETTask<ET.Wait_UnitStop>
	// ET.ETTask<System.ValueTuple<uint,object>>
	// ET.ETTask<byte>
	// ET.ETTask<int>
	// ET.ETTask<object>
	// ET.ETTask<uint>
	// ET.Entity.<>c__60<object>
	// ET.Entity.<>c__61<object,int>
	// ET.Entity.<>c__61<object,object>
	// ET.Entity.<>c__77<object,long,long,int>
	// ET.Entity.<>c__78<object>
	// ET.Entity.<>c__79<object,int>
	// ET.Entity.<>c__79<object,long>
	// ET.Entity.<>c__79<object,object>
	// ET.EntityRef<object>
	// ET.IAwake<int>
	// ET.IAwake<long,long,int>
	// ET.IAwake<long>
	// ET.IAwake<object>
	// ET.IClassEvent<ET.UpdateEvent>
	// ET.ISingletonAwake<object>
	// ET.ListComponent<Unity.Mathematics.float3>
	// ET.MultiMap<int,object>
	// ET.MultiMap<long,long>
	// ET.Singleton<object>
	// ET.StateMachineWrap<ET.A2C_DisconnectHandler.<Run>d__0>
	// ET.StateMachineWrap<ET.A2NetClient_MessageHandler.<Run>d__0>
	// ET.StateMachineWrap<ET.A2NetClient_RequestHandler.<Run>d__0>
	// ET.StateMachineWrap<ET.AI_Attack.<Execute>d__1>
	// ET.StateMachineWrap<ET.AI_XunLuo.<Execute>d__1>
	// ET.StateMachineWrap<ET.AfterCreateClientScene_AddComponent.<Run>d__0>
	// ET.StateMachineWrap<ET.AfterCreateCurrentScene_AddComponent.<Run>d__0>
	// ET.StateMachineWrap<ET.AfterUnitCreate_CreateUnitView.<Run>d__0>
	// ET.StateMachineWrap<ET.AppStartInitFinish_CreateLoginUI.<Run>d__0>
	// ET.StateMachineWrap<ET.ChangePosition_SyncGameObjectPos.<Run>d__0>
	// ET.StateMachineWrap<ET.ChangeRotation_SyncGameObjectRotation.<Run>d__0>
	// ET.StateMachineWrap<ET.ClientSenderComponentSystem.<Call>d__7>
	// ET.StateMachineWrap<ET.ClientSenderComponentSystem.<DisposeAsync>d__3>
	// ET.StateMachineWrap<ET.ClientSenderComponentSystem.<LoginAsync>d__4>
	// ET.StateMachineWrap<ET.ClientSenderComponentSystem.<LoginGameAsync>d__5>
	// ET.StateMachineWrap<ET.ClientSenderComponentSystem.<RemoveFiberAsync>d__2>
	// ET.StateMachineWrap<ET.CoroutineLockComponentSystem.<Wait>d__3>
	// ET.StateMachineWrap<ET.CoroutineLockQueueSystem.<Wait>d__2>
	// ET.StateMachineWrap<ET.CoroutineLockQueueTypeSystem.<Wait>d__4>
	// ET.StateMachineWrap<ET.ETCancellationTokenHelper.<AddCancel>d__1>
	// ET.StateMachineWrap<ET.ETCancellationTokenHelper.<AddCancel>d__2<object>>
	// ET.StateMachineWrap<ET.ETCancellationTokenHelper.<TimeoutAsync>d__0>
	// ET.StateMachineWrap<ET.ETCancellationTokenHelper.<TimeoutAsync>d__3>
	// ET.StateMachineWrap<ET.ETCancellationTokenHelper.<TimeoutAsync>d__4<object>>
	// ET.StateMachineWrap<ET.ETCancellationTokenHelper.<TimeoutAsync>d__5>
	// ET.StateMachineWrap<ET.ETCancellationTokenHelper.<TimeoutAsync>d__6<object>>
	// ET.StateMachineWrap<ET.EnterMapHelper.<EnterMapAsync>d__0>
	// ET.StateMachineWrap<ET.EntryEvent1_InitShare.<Run>d__0>
	// ET.StateMachineWrap<ET.EntryEvent3_InitClient.<Run>d__0>
	// ET.StateMachineWrap<ET.FiberInit_Main.<Handle>d__0>
	// ET.StateMachineWrap<ET.FiberInit_NetClient.<Handle>d__0>
	// ET.StateMachineWrap<ET.GameEntry.<StartAsync>d__1>
	// ET.StateMachineWrap<ET.LoginFinish_CreateLobbyUI.<Run>d__0>
	// ET.StateMachineWrap<ET.LoginHelper.<Login>d__0>
	// ET.StateMachineWrap<ET.M2C_CreateMyUnitHandler.<Run>d__0>
	// ET.StateMachineWrap<ET.M2C_CreateUnitsHandler.<Run>d__0>
	// ET.StateMachineWrap<ET.M2C_PathfindingResultHandler.<Run>d__0>
	// ET.StateMachineWrap<ET.M2C_RemoveUnitsHandler.<Run>d__0>
	// ET.StateMachineWrap<ET.M2C_StartSceneChangeHandler.<Run>d__0>
	// ET.StateMachineWrap<ET.M2C_StopHandler.<Run>d__0>
	// ET.StateMachineWrap<ET.MailBoxType_UnOrderedMessageHandler.<HandleAsync>d__1>
	// ET.StateMachineWrap<ET.Main2NetClient_LoginGameHandler.<Run>d__0>
	// ET.StateMachineWrap<ET.Main2NetClient_LoginHandler.<Run>d__0>
	// ET.StateMachineWrap<ET.MessageDispatcher.<Handle>d__4>
	// ET.StateMachineWrap<ET.MessageHandler.<Handle>d__1<object,object,object>>
	// ET.StateMachineWrap<ET.MessageHandler.<Handle>d__1<object,object>>
	// ET.StateMachineWrap<ET.MessageSenderStruct.<Wait>d__13>
	// ET.StateMachineWrap<ET.MessageSessionHandler.<HandleAsync>d__2<object,object>>
	// ET.StateMachineWrap<ET.MessageSessionHandler.<HandleAsync>d__2<object>>
	// ET.StateMachineWrap<ET.MoveComponentSystem.<MoveToAsync>d__4>
	// ET.StateMachineWrap<ET.MoveHelper.<MoveToAsync>d__0>
	// ET.StateMachineWrap<ET.MoveHelper.<MoveToAsync>d__1>
	// ET.StateMachineWrap<ET.NetClient2Main_SessionDisposeHandler.<Run>d__0>
	// ET.StateMachineWrap<ET.NumericChangeEvent_NotifyWatcher.<Run>d__0>
	// ET.StateMachineWrap<ET.ObjectWaitSystem.<Wait>d__2<object>>
	// ET.StateMachineWrap<ET.OperaComponentSystem.<Test1>d__2>
	// ET.StateMachineWrap<ET.OperaComponentSystem.<Test2>d__3>
	// ET.StateMachineWrap<ET.OperaComponentSystem.<TestCancelAfter>d__4>
	// ET.StateMachineWrap<ET.PingComponentSystem.<PingAsync>d__2>
	// ET.StateMachineWrap<ET.ProcessInnerSenderSystem.<>c__DisplayClass10_0.<<Call>g__Timeout|0>d>
	// ET.StateMachineWrap<ET.ProcessInnerSenderSystem.<Call>d__10>
	// ET.StateMachineWrap<ET.ResourcesLoaderComponentSystem.<LoadAllAssetsAsync>d__4<object>>
	// ET.StateMachineWrap<ET.ResourcesLoaderComponentSystem.<LoadAssetAsync>d__3<object>>
	// ET.StateMachineWrap<ET.ResourcesLoaderComponentSystem.<LoadSceneAsync>d__5>
	// ET.StateMachineWrap<ET.RouterAddressComponentSystem.<GetAllRouter>d__1>
	// ET.StateMachineWrap<ET.RouterAddressComponentSystem.<RefreshRouterAsync>d__2>
	// ET.StateMachineWrap<ET.RouterCheckComponentSystem.<CheckAsync>d__1>
	// ET.StateMachineWrap<ET.RouterHelper.<Connect>d__2>
	// ET.StateMachineWrap<ET.RouterHelper.<CreateRouterSession>d__0>
	// ET.StateMachineWrap<ET.RouterHelper.<GetRouterAddress>d__1>
	// ET.StateMachineWrap<ET.RpcInfo.<Wait>d__7>
	// ET.StateMachineWrap<ET.SceneChangeFinishEvent_CreateUIHelp.<Run>d__0>
	// ET.StateMachineWrap<ET.SceneChangeHelper.<SceneChangeTo>d__0>
	// ET.StateMachineWrap<ET.SceneChangeStart_AddComponent.<Run>d__0>
	// ET.StateMachineWrap<ET.SessionSystem.<Call>d__3>
	// ET.StateMachineWrap<ET.SessionSystem.<Call>d__4>
	// ET.StateMachineWrap<ET.TimerComponentSystem.<WaitAsync>d__10>
	// ET.StateMachineWrap<ET.TimerComponentSystem.<WaitFrameAsync>d__9>
	// ET.StateMachineWrap<ET.TimerComponentSystem.<WaitTillAsync>d__8>
	// ET.StateMachineWrap<ET.WaitCoroutineLock.<Wait>d__5>
	// ET.SystemBase<object,object>
	// ET.UnOrderMultiMap<object,object>
	// ET.UpdateSystem<object>
	// Nino.Core.CachedDeserializer.SubTypeDeserializerWrapper<object,object>
	// Nino.Core.CachedDeserializer<ET.ActorId>
	// Nino.Core.CachedDeserializer<ET.Address>
	// Nino.Core.CachedDeserializer<System.ValueTuple<ET.ActorId,object>>
	// Nino.Core.CachedDeserializer<System.ValueTuple<long,long,int>>
	// Nino.Core.CachedDeserializer<object>
	// Nino.Core.CachedSerializer.SubTypeSerializerWrapper<object,object>
	// Nino.Core.CachedSerializer<ET.ActorId>
	// Nino.Core.CachedSerializer<ET.Address>
	// Nino.Core.CachedSerializer<System.ValueTuple<ET.ActorId,object>>
	// Nino.Core.CachedSerializer<System.ValueTuple<long,long,int>>
	// Nino.Core.CachedSerializer<object>
	// Nino.Core.DeserializeDelegate<ET.ActorId>
	// Nino.Core.DeserializeDelegate<ET.Address>
	// Nino.Core.DeserializeDelegate<System.ValueTuple<ET.ActorId,object>>
	// Nino.Core.DeserializeDelegate<System.ValueTuple<long,long,int>>
	// Nino.Core.DeserializeDelegate<object>
	// Nino.Core.DeserializeDelegateRef<ET.ActorId>
	// Nino.Core.DeserializeDelegateRef<ET.Address>
	// Nino.Core.DeserializeDelegateRef<System.ValueTuple<ET.ActorId,object>>
	// Nino.Core.DeserializeDelegateRef<System.ValueTuple<long,long,int>>
	// Nino.Core.DeserializeDelegateRef<object>
	// Nino.Core.FastMap.Enumerator<System.IntPtr,System.ValueTuple<object,object>>
	// Nino.Core.FastMap.Enumerator<System.IntPtr,byte>
	// Nino.Core.FastMap.Enumerator<System.IntPtr,object>
	// Nino.Core.FastMap.Enumerator<int,System.ValueTuple<object,object>>
	// Nino.Core.FastMap.Enumerator<int,object>
	// Nino.Core.FastMap<System.IntPtr,System.ValueTuple<object,object>>
	// Nino.Core.FastMap<System.IntPtr,byte>
	// Nino.Core.FastMap<System.IntPtr,object>
	// Nino.Core.FastMap<int,System.ValueTuple<object,object>>
	// Nino.Core.FastMap<int,object>
	// Nino.Core.Internal.DictionaryView.Entry<int,long>
	// Nino.Core.Internal.DictionaryView<int,long>
	// Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>
	// Nino.Core.NinoTuple<int,ET.ActorId>
	// Nino.Core.NinoTuple<int,Unity.Mathematics.float3>
	// Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>
	// Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>
	// Nino.Core.NinoTuple<int,int,long,ET.ActorId>
	// Nino.Core.NinoTuple<int,int,long>
	// Nino.Core.NinoTuple<int,int>
	// Nino.Core.NinoTuple<int,long,ET.ActorId>
	// Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>
	// Nino.Core.NinoTuple<int,long,long>
	// Nino.Core.NinoTuple<int,long>
	// Nino.Core.NinoTuple<long,Unity.Mathematics.float3>
	// Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>
	// Nino.Core.NinoTuple<long,long>
	// Nino.Core.SerializeDelegate<ET.ActorId>
	// Nino.Core.SerializeDelegate<ET.Address>
	// Nino.Core.SerializeDelegate<System.ValueTuple<ET.ActorId,object>>
	// Nino.Core.SerializeDelegate<System.ValueTuple<long,long,int>>
	// Nino.Core.SerializeDelegate<object>
	// System.Action<ET.MessageDispatcherInfo>
	// System.Action<ET.MessageInfo>
	// System.Action<ET.MessageSessionDispatcherInfo>
	// System.Action<ET.NumericWatcherInfo>
	// System.Action<Unity.Mathematics.float3>
	// System.Action<byte>
	// System.Action<int>
	// System.Action<long,int>
	// System.Action<long,object>
	// System.Action<long>
	// System.Action<object,int>
	// System.Action<object,long,long,int>
	// System.Action<object,long>
	// System.Action<object,object>
	// System.Action<object>
	// System.ArraySegment.Enumerator<ET.ActorId>
	// System.ArraySegment.Enumerator<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>>
	// System.ArraySegment.Enumerator<Nino.Core.NinoTuple<int,ET.ActorId>>
	// System.ArraySegment.Enumerator<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>>
	// System.ArraySegment.Enumerator<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>>
	// System.ArraySegment.Enumerator<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>>
	// System.ArraySegment.Enumerator<Nino.Core.NinoTuple<int,int,long,ET.ActorId>>
	// System.ArraySegment.Enumerator<Nino.Core.NinoTuple<int,int,long>>
	// System.ArraySegment.Enumerator<Nino.Core.NinoTuple<int,int>>
	// System.ArraySegment.Enumerator<Nino.Core.NinoTuple<int,long,ET.ActorId>>
	// System.ArraySegment.Enumerator<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>>
	// System.ArraySegment.Enumerator<Nino.Core.NinoTuple<int,long,long>>
	// System.ArraySegment.Enumerator<Nino.Core.NinoTuple<int,long>>
	// System.ArraySegment.Enumerator<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>>
	// System.ArraySegment.Enumerator<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>>
	// System.ArraySegment.Enumerator<Nino.Core.NinoTuple<long,long>>
	// System.ArraySegment.Enumerator<System.Collections.Generic.KeyValuePair<int,long>>
	// System.ArraySegment.Enumerator<System.ValueTuple<long,long,int>>
	// System.ArraySegment.Enumerator<Unity.Mathematics.float3>
	// System.ArraySegment.Enumerator<byte>
	// System.ArraySegment.Enumerator<int>
	// System.ArraySegment.Enumerator<long>
	// System.ArraySegment.Enumerator<object>
	// System.ArraySegment.Enumerator<uint>
	// System.ArraySegment<ET.ActorId>
	// System.ArraySegment<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>>
	// System.ArraySegment<Nino.Core.NinoTuple<int,ET.ActorId>>
	// System.ArraySegment<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>>
	// System.ArraySegment<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>>
	// System.ArraySegment<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>>
	// System.ArraySegment<Nino.Core.NinoTuple<int,int,long,ET.ActorId>>
	// System.ArraySegment<Nino.Core.NinoTuple<int,int,long>>
	// System.ArraySegment<Nino.Core.NinoTuple<int,int>>
	// System.ArraySegment<Nino.Core.NinoTuple<int,long,ET.ActorId>>
	// System.ArraySegment<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>>
	// System.ArraySegment<Nino.Core.NinoTuple<int,long,long>>
	// System.ArraySegment<Nino.Core.NinoTuple<int,long>>
	// System.ArraySegment<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>>
	// System.ArraySegment<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>>
	// System.ArraySegment<Nino.Core.NinoTuple<long,long>>
	// System.ArraySegment<System.Collections.Generic.KeyValuePair<int,long>>
	// System.ArraySegment<System.ValueTuple<long,long,int>>
	// System.ArraySegment<Unity.Mathematics.float3>
	// System.ArraySegment<byte>
	// System.ArraySegment<int>
	// System.ArraySegment<long>
	// System.ArraySegment<object>
	// System.ArraySegment<uint>
	// System.Buffers.IBufferWriter<byte>
	// System.ByReference<ET.ActorId>
	// System.ByReference<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>>
	// System.ByReference<Nino.Core.NinoTuple<int,ET.ActorId>>
	// System.ByReference<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>>
	// System.ByReference<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>>
	// System.ByReference<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>>
	// System.ByReference<Nino.Core.NinoTuple<int,int,long,ET.ActorId>>
	// System.ByReference<Nino.Core.NinoTuple<int,int,long>>
	// System.ByReference<Nino.Core.NinoTuple<int,int>>
	// System.ByReference<Nino.Core.NinoTuple<int,long,ET.ActorId>>
	// System.ByReference<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>>
	// System.ByReference<Nino.Core.NinoTuple<int,long,long>>
	// System.ByReference<Nino.Core.NinoTuple<int,long>>
	// System.ByReference<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>>
	// System.ByReference<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>>
	// System.ByReference<Nino.Core.NinoTuple<long,long>>
	// System.ByReference<System.Collections.Generic.KeyValuePair<int,long>>
	// System.ByReference<System.ValueTuple<long,long,int>>
	// System.ByReference<Unity.Mathematics.float3>
	// System.ByReference<byte>
	// System.ByReference<int>
	// System.ByReference<long>
	// System.ByReference<object>
	// System.ByReference<uint>
	// System.Collections.Concurrent.ConcurrentDictionary.<GetEnumerator>d__35<int,object>
	// System.Collections.Concurrent.ConcurrentDictionary.DictionaryEnumerator<int,object>
	// System.Collections.Concurrent.ConcurrentDictionary.Node<int,object>
	// System.Collections.Concurrent.ConcurrentDictionary.Tables<int,object>
	// System.Collections.Concurrent.ConcurrentDictionary<int,object>
	// System.Collections.Concurrent.ConcurrentQueue.<Enumerate>d__28<ET.MessageInfo>
	// System.Collections.Concurrent.ConcurrentQueue.<Enumerate>d__28<object>
	// System.Collections.Concurrent.ConcurrentQueue.Segment<ET.MessageInfo>
	// System.Collections.Concurrent.ConcurrentQueue.Segment<object>
	// System.Collections.Concurrent.ConcurrentQueue<ET.MessageInfo>
	// System.Collections.Concurrent.ConcurrentQueue<object>
	// System.Collections.Generic.ArraySortHelper<ET.MessageDispatcherInfo>
	// System.Collections.Generic.ArraySortHelper<ET.MessageInfo>
	// System.Collections.Generic.ArraySortHelper<ET.MessageSessionDispatcherInfo>
	// System.Collections.Generic.ArraySortHelper<ET.NumericWatcherInfo>
	// System.Collections.Generic.ArraySortHelper<Unity.Mathematics.float3>
	// System.Collections.Generic.ArraySortHelper<int>
	// System.Collections.Generic.ArraySortHelper<long>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<ET.ActorId>
	// System.Collections.Generic.Comparer<ET.MessageDispatcherInfo>
	// System.Collections.Generic.Comparer<ET.MessageInfo>
	// System.Collections.Generic.Comparer<ET.MessageSessionDispatcherInfo>
	// System.Collections.Generic.Comparer<ET.NumericWatcherInfo>
	// System.Collections.Generic.Comparer<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.Comparer<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.Comparer<Unity.Mathematics.float3>
	// System.Collections.Generic.Comparer<int>
	// System.Collections.Generic.Comparer<long>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Comparer<uint>
	// System.Collections.Generic.Comparer<ushort>
	// System.Collections.Generic.Dictionary.Enumerator<int,ET.MessageSenderStruct>
	// System.Collections.Generic.Dictionary.Enumerator<int,ET.RpcInfo>
	// System.Collections.Generic.Dictionary.Enumerator<int,long>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<long,ET.TimerAction>
	// System.Collections.Generic.Dictionary.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,ushort>
	// System.Collections.Generic.Dictionary.Enumerator<ushort,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,ET.MessageSenderStruct>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,ET.RpcInfo>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,long>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<long,ET.TimerAction>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,ushort>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<ushort,object>
	// System.Collections.Generic.Dictionary.KeyCollection<int,ET.MessageSenderStruct>
	// System.Collections.Generic.Dictionary.KeyCollection<int,ET.RpcInfo>
	// System.Collections.Generic.Dictionary.KeyCollection<int,long>
	// System.Collections.Generic.Dictionary.KeyCollection<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection<long,ET.TimerAction>
	// System.Collections.Generic.Dictionary.KeyCollection<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,ushort>
	// System.Collections.Generic.Dictionary.KeyCollection<ushort,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,ET.MessageSenderStruct>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,ET.RpcInfo>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,long>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<long,ET.TimerAction>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,ushort>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<ushort,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,ET.MessageSenderStruct>
	// System.Collections.Generic.Dictionary.ValueCollection<int,ET.RpcInfo>
	// System.Collections.Generic.Dictionary.ValueCollection<int,long>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<long,ET.TimerAction>
	// System.Collections.Generic.Dictionary.ValueCollection<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,ushort>
	// System.Collections.Generic.Dictionary.ValueCollection<ushort,object>
	// System.Collections.Generic.Dictionary<int,ET.MessageSenderStruct>
	// System.Collections.Generic.Dictionary<int,ET.RpcInfo>
	// System.Collections.Generic.Dictionary<int,long>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<long,ET.TimerAction>
	// System.Collections.Generic.Dictionary<long,object>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.Dictionary<object,ushort>
	// System.Collections.Generic.Dictionary<ushort,object>
	// System.Collections.Generic.EqualityComparer<ET.ActorId>
	// System.Collections.Generic.EqualityComparer<ET.MessageSenderStruct>
	// System.Collections.Generic.EqualityComparer<ET.RpcInfo>
	// System.Collections.Generic.EqualityComparer<ET.TimerAction>
	// System.Collections.Generic.EqualityComparer<System.IntPtr>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<object,object>>
	// System.Collections.Generic.EqualityComparer<byte>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<long>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.EqualityComparer<uint>
	// System.Collections.Generic.EqualityComparer<ushort>
	// System.Collections.Generic.HashSet.Enumerator<object>
	// System.Collections.Generic.HashSet<object>
	// System.Collections.Generic.HashSetEqualityComparer<object>
	// System.Collections.Generic.ICollection<ET.MessageDispatcherInfo>
	// System.Collections.Generic.ICollection<ET.MessageInfo>
	// System.Collections.Generic.ICollection<ET.MessageSessionDispatcherInfo>
	// System.Collections.Generic.ICollection<ET.NumericWatcherInfo>
	// System.Collections.Generic.ICollection<ET.RpcInfo>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,ET.MessageSenderStruct>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,ET.RpcInfo>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,long>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<long,ET.TimerAction>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,ushort>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<ushort,object>>
	// System.Collections.Generic.ICollection<Unity.Mathematics.float3>
	// System.Collections.Generic.ICollection<int>
	// System.Collections.Generic.ICollection<long>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<ET.MessageDispatcherInfo>
	// System.Collections.Generic.IComparer<ET.MessageInfo>
	// System.Collections.Generic.IComparer<ET.MessageSessionDispatcherInfo>
	// System.Collections.Generic.IComparer<ET.NumericWatcherInfo>
	// System.Collections.Generic.IComparer<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IComparer<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IComparer<Unity.Mathematics.float3>
	// System.Collections.Generic.IComparer<int>
	// System.Collections.Generic.IComparer<long>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IDictionary<int,object>
	// System.Collections.Generic.IDictionary<long,object>
	// System.Collections.Generic.IEnumerable<ET.MessageDispatcherInfo>
	// System.Collections.Generic.IEnumerable<ET.MessageInfo>
	// System.Collections.Generic.IEnumerable<ET.MessageSessionDispatcherInfo>
	// System.Collections.Generic.IEnumerable<ET.NumericWatcherInfo>
	// System.Collections.Generic.IEnumerable<ET.RpcInfo>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,ET.MessageSenderStruct>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,ET.RpcInfo>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,long>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,ET.TimerAction>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,ushort>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<ushort,object>>
	// System.Collections.Generic.IEnumerable<Unity.Mathematics.float3>
	// System.Collections.Generic.IEnumerable<int>
	// System.Collections.Generic.IEnumerable<long>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<ET.MessageDispatcherInfo>
	// System.Collections.Generic.IEnumerator<ET.MessageInfo>
	// System.Collections.Generic.IEnumerator<ET.MessageSessionDispatcherInfo>
	// System.Collections.Generic.IEnumerator<ET.NumericWatcherInfo>
	// System.Collections.Generic.IEnumerator<ET.RpcInfo>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,ET.MessageSenderStruct>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,ET.RpcInfo>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,long>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long,ET.TimerAction>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,ushort>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<ushort,object>>
	// System.Collections.Generic.IEnumerator<Unity.Mathematics.float3>
	// System.Collections.Generic.IEnumerator<int>
	// System.Collections.Generic.IEnumerator<long>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<System.IntPtr>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<long>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IEqualityComparer<ushort>
	// System.Collections.Generic.IList<ET.MessageDispatcherInfo>
	// System.Collections.Generic.IList<ET.MessageInfo>
	// System.Collections.Generic.IList<ET.MessageSessionDispatcherInfo>
	// System.Collections.Generic.IList<ET.NumericWatcherInfo>
	// System.Collections.Generic.IList<Unity.Mathematics.float3>
	// System.Collections.Generic.IList<int>
	// System.Collections.Generic.IList<long>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.KeyValuePair<System.IntPtr,System.ValueTuple<object,object>>
	// System.Collections.Generic.KeyValuePair<System.IntPtr,byte>
	// System.Collections.Generic.KeyValuePair<System.IntPtr,object>
	// System.Collections.Generic.KeyValuePair<int,ET.MessageSenderStruct>
	// System.Collections.Generic.KeyValuePair<int,ET.RpcInfo>
	// System.Collections.Generic.KeyValuePair<int,System.ValueTuple<object,object>>
	// System.Collections.Generic.KeyValuePair<int,long>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<long,ET.TimerAction>
	// System.Collections.Generic.KeyValuePair<long,object>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.KeyValuePair<object,ushort>
	// System.Collections.Generic.KeyValuePair<ushort,object>
	// System.Collections.Generic.List.Enumerator<ET.MessageDispatcherInfo>
	// System.Collections.Generic.List.Enumerator<ET.MessageInfo>
	// System.Collections.Generic.List.Enumerator<ET.MessageSessionDispatcherInfo>
	// System.Collections.Generic.List.Enumerator<ET.NumericWatcherInfo>
	// System.Collections.Generic.List.Enumerator<Unity.Mathematics.float3>
	// System.Collections.Generic.List.Enumerator<int>
	// System.Collections.Generic.List.Enumerator<long>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<ET.MessageDispatcherInfo>
	// System.Collections.Generic.List<ET.MessageInfo>
	// System.Collections.Generic.List<ET.MessageSessionDispatcherInfo>
	// System.Collections.Generic.List<ET.NumericWatcherInfo>
	// System.Collections.Generic.List<Unity.Mathematics.float3>
	// System.Collections.Generic.List<int>
	// System.Collections.Generic.List<long>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<ET.ActorId>
	// System.Collections.Generic.ObjectComparer<ET.MessageDispatcherInfo>
	// System.Collections.Generic.ObjectComparer<ET.MessageInfo>
	// System.Collections.Generic.ObjectComparer<ET.MessageSessionDispatcherInfo>
	// System.Collections.Generic.ObjectComparer<ET.NumericWatcherInfo>
	// System.Collections.Generic.ObjectComparer<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.ObjectComparer<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.ObjectComparer<Unity.Mathematics.float3>
	// System.Collections.Generic.ObjectComparer<int>
	// System.Collections.Generic.ObjectComparer<long>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectComparer<uint>
	// System.Collections.Generic.ObjectComparer<ushort>
	// System.Collections.Generic.ObjectEqualityComparer<ET.ActorId>
	// System.Collections.Generic.ObjectEqualityComparer<ET.MessageSenderStruct>
	// System.Collections.Generic.ObjectEqualityComparer<ET.RpcInfo>
	// System.Collections.Generic.ObjectEqualityComparer<ET.TimerAction>
	// System.Collections.Generic.ObjectEqualityComparer<System.IntPtr>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<object,object>>
	// System.Collections.Generic.ObjectEqualityComparer<byte>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<long>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<uint>
	// System.Collections.Generic.ObjectEqualityComparer<ushort>
	// System.Collections.Generic.Queue.Enumerator<System.ValueTuple<long,long,int>>
	// System.Collections.Generic.Queue.Enumerator<long>
	// System.Collections.Generic.Queue.Enumerator<object>
	// System.Collections.Generic.Queue<System.ValueTuple<long,long,int>>
	// System.Collections.Generic.Queue<long>
	// System.Collections.Generic.Queue<object>
	// System.Collections.Generic.SortedDictionary.<>c__DisplayClass34_0<int,object>
	// System.Collections.Generic.SortedDictionary.<>c__DisplayClass34_0<long,object>
	// System.Collections.Generic.SortedDictionary.<>c__DisplayClass34_1<int,object>
	// System.Collections.Generic.SortedDictionary.<>c__DisplayClass34_1<long,object>
	// System.Collections.Generic.SortedDictionary.Enumerator<int,object>
	// System.Collections.Generic.SortedDictionary.Enumerator<long,object>
	// System.Collections.Generic.SortedDictionary.KeyCollection.<>c__DisplayClass5_0<int,object>
	// System.Collections.Generic.SortedDictionary.KeyCollection.<>c__DisplayClass5_0<long,object>
	// System.Collections.Generic.SortedDictionary.KeyCollection.<>c__DisplayClass6_0<int,object>
	// System.Collections.Generic.SortedDictionary.KeyCollection.<>c__DisplayClass6_0<long,object>
	// System.Collections.Generic.SortedDictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.SortedDictionary.KeyCollection.Enumerator<long,object>
	// System.Collections.Generic.SortedDictionary.KeyCollection<int,object>
	// System.Collections.Generic.SortedDictionary.KeyCollection<long,object>
	// System.Collections.Generic.SortedDictionary.KeyValuePairComparer<int,object>
	// System.Collections.Generic.SortedDictionary.KeyValuePairComparer<long,object>
	// System.Collections.Generic.SortedDictionary.ValueCollection.<>c__DisplayClass5_0<int,object>
	// System.Collections.Generic.SortedDictionary.ValueCollection.<>c__DisplayClass5_0<long,object>
	// System.Collections.Generic.SortedDictionary.ValueCollection.<>c__DisplayClass6_0<int,object>
	// System.Collections.Generic.SortedDictionary.ValueCollection.<>c__DisplayClass6_0<long,object>
	// System.Collections.Generic.SortedDictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.SortedDictionary.ValueCollection.Enumerator<long,object>
	// System.Collections.Generic.SortedDictionary.ValueCollection<int,object>
	// System.Collections.Generic.SortedDictionary.ValueCollection<long,object>
	// System.Collections.Generic.SortedDictionary<int,object>
	// System.Collections.Generic.SortedDictionary<long,object>
	// System.Collections.Generic.SortedSet.<>c__DisplayClass52_0<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.SortedSet.<>c__DisplayClass52_0<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.SortedSet.<>c__DisplayClass53_0<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.SortedSet.<>c__DisplayClass53_0<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.SortedSet.Enumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.SortedSet.Enumerator<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.SortedSet.Node<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.SortedSet.Node<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.SortedSet<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.SortedSet<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.Stack.Enumerator<object>
	// System.Collections.Generic.Stack<object>
	// System.Collections.Generic.TreeSet<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.TreeSet<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.TreeWalkPredicate<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.TreeWalkPredicate<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.ObjectModel.ReadOnlyCollection<ET.MessageDispatcherInfo>
	// System.Collections.ObjectModel.ReadOnlyCollection<ET.MessageInfo>
	// System.Collections.ObjectModel.ReadOnlyCollection<ET.MessageSessionDispatcherInfo>
	// System.Collections.ObjectModel.ReadOnlyCollection<ET.NumericWatcherInfo>
	// System.Collections.ObjectModel.ReadOnlyCollection<Unity.Mathematics.float3>
	// System.Collections.ObjectModel.ReadOnlyCollection<int>
	// System.Collections.ObjectModel.ReadOnlyCollection<long>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<ET.MessageDispatcherInfo>
	// System.Comparison<ET.MessageInfo>
	// System.Comparison<ET.MessageSessionDispatcherInfo>
	// System.Comparison<ET.NumericWatcherInfo>
	// System.Comparison<Unity.Mathematics.float3>
	// System.Comparison<int>
	// System.Comparison<long>
	// System.Comparison<object>
	// System.Func<byte,object>
	// System.Func<int,object,object>
	// System.Func<int,object>
	// System.Func<object,object>
	// System.Linq.Buffer<ET.RpcInfo>
	// System.Linq.Buffer<object>
	// System.Predicate<ET.MessageDispatcherInfo>
	// System.Predicate<ET.MessageInfo>
	// System.Predicate<ET.MessageSessionDispatcherInfo>
	// System.Predicate<ET.NumericWatcherInfo>
	// System.Predicate<Unity.Mathematics.float3>
	// System.Predicate<int>
	// System.Predicate<long>
	// System.Predicate<object>
	// System.ReadOnlySpan.Enumerator<ET.ActorId>
	// System.ReadOnlySpan.Enumerator<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>>
	// System.ReadOnlySpan.Enumerator<Nino.Core.NinoTuple<int,ET.ActorId>>
	// System.ReadOnlySpan.Enumerator<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>>
	// System.ReadOnlySpan.Enumerator<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>>
	// System.ReadOnlySpan.Enumerator<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>>
	// System.ReadOnlySpan.Enumerator<Nino.Core.NinoTuple<int,int,long,ET.ActorId>>
	// System.ReadOnlySpan.Enumerator<Nino.Core.NinoTuple<int,int,long>>
	// System.ReadOnlySpan.Enumerator<Nino.Core.NinoTuple<int,int>>
	// System.ReadOnlySpan.Enumerator<Nino.Core.NinoTuple<int,long,ET.ActorId>>
	// System.ReadOnlySpan.Enumerator<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>>
	// System.ReadOnlySpan.Enumerator<Nino.Core.NinoTuple<int,long,long>>
	// System.ReadOnlySpan.Enumerator<Nino.Core.NinoTuple<int,long>>
	// System.ReadOnlySpan.Enumerator<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>>
	// System.ReadOnlySpan.Enumerator<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>>
	// System.ReadOnlySpan.Enumerator<Nino.Core.NinoTuple<long,long>>
	// System.ReadOnlySpan.Enumerator<System.Collections.Generic.KeyValuePair<int,long>>
	// System.ReadOnlySpan.Enumerator<System.ValueTuple<long,long,int>>
	// System.ReadOnlySpan.Enumerator<Unity.Mathematics.float3>
	// System.ReadOnlySpan.Enumerator<byte>
	// System.ReadOnlySpan.Enumerator<int>
	// System.ReadOnlySpan.Enumerator<long>
	// System.ReadOnlySpan.Enumerator<object>
	// System.ReadOnlySpan.Enumerator<uint>
	// System.ReadOnlySpan<ET.ActorId>
	// System.ReadOnlySpan<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>>
	// System.ReadOnlySpan<Nino.Core.NinoTuple<int,ET.ActorId>>
	// System.ReadOnlySpan<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>>
	// System.ReadOnlySpan<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>>
	// System.ReadOnlySpan<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>>
	// System.ReadOnlySpan<Nino.Core.NinoTuple<int,int,long,ET.ActorId>>
	// System.ReadOnlySpan<Nino.Core.NinoTuple<int,int,long>>
	// System.ReadOnlySpan<Nino.Core.NinoTuple<int,int>>
	// System.ReadOnlySpan<Nino.Core.NinoTuple<int,long,ET.ActorId>>
	// System.ReadOnlySpan<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>>
	// System.ReadOnlySpan<Nino.Core.NinoTuple<int,long,long>>
	// System.ReadOnlySpan<Nino.Core.NinoTuple<int,long>>
	// System.ReadOnlySpan<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>>
	// System.ReadOnlySpan<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>>
	// System.ReadOnlySpan<Nino.Core.NinoTuple<long,long>>
	// System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<int,long>>
	// System.ReadOnlySpan<System.ValueTuple<long,long,int>>
	// System.ReadOnlySpan<Unity.Mathematics.float3>
	// System.ReadOnlySpan<byte>
	// System.ReadOnlySpan<int>
	// System.ReadOnlySpan<long>
	// System.ReadOnlySpan<object>
	// System.ReadOnlySpan<uint>
	// System.Span.Enumerator<ET.ActorId>
	// System.Span.Enumerator<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>>
	// System.Span.Enumerator<Nino.Core.NinoTuple<int,ET.ActorId>>
	// System.Span.Enumerator<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>>
	// System.Span.Enumerator<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>>
	// System.Span.Enumerator<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>>
	// System.Span.Enumerator<Nino.Core.NinoTuple<int,int,long,ET.ActorId>>
	// System.Span.Enumerator<Nino.Core.NinoTuple<int,int,long>>
	// System.Span.Enumerator<Nino.Core.NinoTuple<int,int>>
	// System.Span.Enumerator<Nino.Core.NinoTuple<int,long,ET.ActorId>>
	// System.Span.Enumerator<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>>
	// System.Span.Enumerator<Nino.Core.NinoTuple<int,long,long>>
	// System.Span.Enumerator<Nino.Core.NinoTuple<int,long>>
	// System.Span.Enumerator<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>>
	// System.Span.Enumerator<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>>
	// System.Span.Enumerator<Nino.Core.NinoTuple<long,long>>
	// System.Span.Enumerator<System.Collections.Generic.KeyValuePair<int,long>>
	// System.Span.Enumerator<System.ValueTuple<long,long,int>>
	// System.Span.Enumerator<Unity.Mathematics.float3>
	// System.Span.Enumerator<byte>
	// System.Span.Enumerator<int>
	// System.Span.Enumerator<long>
	// System.Span.Enumerator<object>
	// System.Span.Enumerator<uint>
	// System.Span<ET.ActorId>
	// System.Span<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>>
	// System.Span<Nino.Core.NinoTuple<int,ET.ActorId>>
	// System.Span<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>>
	// System.Span<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>>
	// System.Span<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>>
	// System.Span<Nino.Core.NinoTuple<int,int,long,ET.ActorId>>
	// System.Span<Nino.Core.NinoTuple<int,int,long>>
	// System.Span<Nino.Core.NinoTuple<int,int>>
	// System.Span<Nino.Core.NinoTuple<int,long,ET.ActorId>>
	// System.Span<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>>
	// System.Span<Nino.Core.NinoTuple<int,long,long>>
	// System.Span<Nino.Core.NinoTuple<int,long>>
	// System.Span<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>>
	// System.Span<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>>
	// System.Span<Nino.Core.NinoTuple<long,long>>
	// System.Span<System.Collections.Generic.KeyValuePair<int,long>>
	// System.Span<System.ValueTuple<long,long,int>>
	// System.Span<Unity.Mathematics.float3>
	// System.Span<byte>
	// System.Span<int>
	// System.Span<long>
	// System.Span<object>
	// System.Span<uint>
	// System.ValueTuple<ET.ActorId,object>
	// System.ValueTuple<long,long,int>
	// System.ValueTuple<object,object>
	// System.ValueTuple<uint,object>
	// System.ValueTuple<uint,uint>
	// System.ValueTuple<ushort,object>
	// }}

	public void RefMethods()
	{
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,ET.AppStartInitFinish_CreateLoginUI.<Run>d__0>(Cysharp.Threading.Tasks.UniTask.Awaiter&,ET.AppStartInitFinish_CreateLoginUI.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,ET.LoginFinish_CreateLobbyUI.<Run>d__0>(Cysharp.Threading.Tasks.UniTask.Awaiter&,ET.LoginFinish_CreateLobbyUI.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,ET.SceneChangeFinishEvent_CreateUIHelp.<Run>d__0>(Cysharp.Threading.Tasks.UniTask.Awaiter&,ET.SceneChangeFinishEvent_CreateUIHelp.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,ET.ResourcesLoaderComponentSystem.<LoadSceneAsync>d__5>(System.Runtime.CompilerServices.TaskAwaiter&,ET.ResourcesLoaderComponentSystem.<LoadSceneAsync>d__5&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.A2C_DisconnectHandler.<Run>d__0>(object&,ET.A2C_DisconnectHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.A2NetClient_MessageHandler.<Run>d__0>(object&,ET.A2NetClient_MessageHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.A2NetClient_RequestHandler.<Run>d__0>(object&,ET.A2NetClient_RequestHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.AI_Attack.<Execute>d__1>(object&,ET.AI_Attack.<Execute>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.AI_XunLuo.<Execute>d__1>(object&,ET.AI_XunLuo.<Execute>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.AfterCreateClientScene_AddComponent.<Run>d__0>(object&,ET.AfterCreateClientScene_AddComponent.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.AfterCreateCurrentScene_AddComponent.<Run>d__0>(object&,ET.AfterCreateCurrentScene_AddComponent.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.AfterUnitCreate_CreateUnitView.<Run>d__0>(object&,ET.AfterUnitCreate_CreateUnitView.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.ChangePosition_SyncGameObjectPos.<Run>d__0>(object&,ET.ChangePosition_SyncGameObjectPos.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.ChangeRotation_SyncGameObjectRotation.<Run>d__0>(object&,ET.ChangeRotation_SyncGameObjectRotation.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.ClientSenderComponentSystem.<DisposeAsync>d__3>(object&,ET.ClientSenderComponentSystem.<DisposeAsync>d__3&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.ClientSenderComponentSystem.<RemoveFiberAsync>d__2>(object&,ET.ClientSenderComponentSystem.<RemoveFiberAsync>d__2&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.ETCancellationTokenHelper.<AddCancel>d__1>(object&,ET.ETCancellationTokenHelper.<AddCancel>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.ETCancellationTokenHelper.<TimeoutAsync>d__0>(object&,ET.ETCancellationTokenHelper.<TimeoutAsync>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.ETCancellationTokenHelper.<TimeoutAsync>d__3>(object&,ET.ETCancellationTokenHelper.<TimeoutAsync>d__3&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.ETCancellationTokenHelper.<TimeoutAsync>d__5>(object&,ET.ETCancellationTokenHelper.<TimeoutAsync>d__5&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.EnterMapHelper.<EnterMapAsync>d__0>(object&,ET.EnterMapHelper.<EnterMapAsync>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.EntryEvent1_InitShare.<Run>d__0>(object&,ET.EntryEvent1_InitShare.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.EntryEvent3_InitClient.<Run>d__0>(object&,ET.EntryEvent3_InitClient.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.FiberInit_Main.<Handle>d__0>(object&,ET.FiberInit_Main.<Handle>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.FiberInit_NetClient.<Handle>d__0>(object&,ET.FiberInit_NetClient.<Handle>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.GameEntry.<StartAsync>d__1>(object&,ET.GameEntry.<StartAsync>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.LoginHelper.<Login>d__0>(object&,ET.LoginHelper.<Login>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.M2C_CreateMyUnitHandler.<Run>d__0>(object&,ET.M2C_CreateMyUnitHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.M2C_CreateUnitsHandler.<Run>d__0>(object&,ET.M2C_CreateUnitsHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.M2C_PathfindingResultHandler.<Run>d__0>(object&,ET.M2C_PathfindingResultHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.M2C_RemoveUnitsHandler.<Run>d__0>(object&,ET.M2C_RemoveUnitsHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.M2C_StartSceneChangeHandler.<Run>d__0>(object&,ET.M2C_StartSceneChangeHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.M2C_StopHandler.<Run>d__0>(object&,ET.M2C_StopHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.MailBoxType_UnOrderedMessageHandler.<HandleAsync>d__1>(object&,ET.MailBoxType_UnOrderedMessageHandler.<HandleAsync>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.Main2NetClient_LoginGameHandler.<Run>d__0>(object&,ET.Main2NetClient_LoginGameHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.Main2NetClient_LoginHandler.<Run>d__0>(object&,ET.Main2NetClient_LoginHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.MessageDispatcher.<Handle>d__4>(object&,ET.MessageDispatcher.<Handle>d__4&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.MessageHandler.<Handle>d__1<object,object,object>>(object&,ET.MessageHandler.<Handle>d__1<object,object,object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.MessageHandler.<Handle>d__1<object,object>>(object&,ET.MessageHandler.<Handle>d__1<object,object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.MessageSessionHandler.<HandleAsync>d__2<object,object>>(object&,ET.MessageSessionHandler.<HandleAsync>d__2<object,object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.MessageSessionHandler.<HandleAsync>d__2<object>>(object&,ET.MessageSessionHandler.<HandleAsync>d__2<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.MoveHelper.<MoveToAsync>d__1>(object&,ET.MoveHelper.<MoveToAsync>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.NetClient2Main_SessionDisposeHandler.<Run>d__0>(object&,ET.NetClient2Main_SessionDisposeHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.NumericChangeEvent_NotifyWatcher.<Run>d__0>(object&,ET.NumericChangeEvent_NotifyWatcher.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.OperaComponentSystem.<Test1>d__2>(object&,ET.OperaComponentSystem.<Test1>d__2&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.OperaComponentSystem.<Test2>d__3>(object&,ET.OperaComponentSystem.<Test2>d__3&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.OperaComponentSystem.<TestCancelAfter>d__4>(object&,ET.OperaComponentSystem.<TestCancelAfter>d__4&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.PingComponentSystem.<PingAsync>d__2>(object&,ET.PingComponentSystem.<PingAsync>d__2&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.ProcessInnerSenderSystem.<>c__DisplayClass10_0.<<Call>g__Timeout|0>d>(object&,ET.ProcessInnerSenderSystem.<>c__DisplayClass10_0.<<Call>g__Timeout|0>d&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.ResourcesLoaderComponentSystem.<LoadSceneAsync>d__5>(object&,ET.ResourcesLoaderComponentSystem.<LoadSceneAsync>d__5&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.RouterAddressComponentSystem.<GetAllRouter>d__1>(object&,ET.RouterAddressComponentSystem.<GetAllRouter>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.RouterAddressComponentSystem.<RefreshRouterAsync>d__2>(object&,ET.RouterAddressComponentSystem.<RefreshRouterAsync>d__2&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.RouterCheckComponentSystem.<CheckAsync>d__1>(object&,ET.RouterCheckComponentSystem.<CheckAsync>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.SceneChangeHelper.<SceneChangeTo>d__0>(object&,ET.SceneChangeHelper.<SceneChangeTo>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.SceneChangeStart_AddComponent.<Run>d__0>(object&,ET.SceneChangeStart_AddComponent.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.TimerComponentSystem.<WaitAsync>d__10>(object&,ET.TimerComponentSystem.<WaitAsync>d__10&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.TimerComponentSystem.<WaitFrameAsync>d__9>(object&,ET.TimerComponentSystem.<WaitFrameAsync>d__9&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,ET.TimerComponentSystem.<WaitTillAsync>d__8>(object&,ET.TimerComponentSystem.<WaitTillAsync>d__8&)
		// System.Void ET.ETAsyncTaskMethodBuilder<System.ValueTuple<uint,object>>.AwaitUnsafeOnCompleted<object,ET.RouterHelper.<GetRouterAddress>d__1>(object&,ET.RouterHelper.<GetRouterAddress>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder<byte>.AwaitUnsafeOnCompleted<object,ET.MoveComponentSystem.<MoveToAsync>d__4>(object&,ET.MoveComponentSystem.<MoveToAsync>d__4&)
		// System.Void ET.ETAsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<object,ET.MoveHelper.<MoveToAsync>d__0>(object&,ET.MoveHelper.<MoveToAsync>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,ET.ResourcesLoaderComponentSystem.<LoadAllAssetsAsync>d__4<object>>(System.Runtime.CompilerServices.TaskAwaiter&,ET.ResourcesLoaderComponentSystem.<LoadAllAssetsAsync>d__4<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,ET.ResourcesLoaderComponentSystem.<LoadAssetAsync>d__3<object>>(System.Runtime.CompilerServices.TaskAwaiter&,ET.ResourcesLoaderComponentSystem.<LoadAssetAsync>d__3<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.ClientSenderComponentSystem.<Call>d__7>(object&,ET.ClientSenderComponentSystem.<Call>d__7&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.ClientSenderComponentSystem.<LoginAsync>d__4>(object&,ET.ClientSenderComponentSystem.<LoginAsync>d__4&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.ClientSenderComponentSystem.<LoginGameAsync>d__5>(object&,ET.ClientSenderComponentSystem.<LoginGameAsync>d__5&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.CoroutineLockComponentSystem.<Wait>d__3>(object&,ET.CoroutineLockComponentSystem.<Wait>d__3&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.CoroutineLockQueueSystem.<Wait>d__2>(object&,ET.CoroutineLockQueueSystem.<Wait>d__2&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.CoroutineLockQueueTypeSystem.<Wait>d__4>(object&,ET.CoroutineLockQueueTypeSystem.<Wait>d__4&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.ETCancellationTokenHelper.<AddCancel>d__2<object>>(object&,ET.ETCancellationTokenHelper.<AddCancel>d__2<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.ETCancellationTokenHelper.<TimeoutAsync>d__4<object>>(object&,ET.ETCancellationTokenHelper.<TimeoutAsync>d__4<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.ETCancellationTokenHelper.<TimeoutAsync>d__6<object>>(object&,ET.ETCancellationTokenHelper.<TimeoutAsync>d__6<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.MessageSenderStruct.<Wait>d__13>(object&,ET.MessageSenderStruct.<Wait>d__13&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.ObjectWaitSystem.<Wait>d__2<object>>(object&,ET.ObjectWaitSystem.<Wait>d__2<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.ProcessInnerSenderSystem.<Call>d__10>(object&,ET.ProcessInnerSenderSystem.<Call>d__10&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.ResourcesLoaderComponentSystem.<LoadAllAssetsAsync>d__4<object>>(object&,ET.ResourcesLoaderComponentSystem.<LoadAllAssetsAsync>d__4<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.ResourcesLoaderComponentSystem.<LoadAssetAsync>d__3<object>>(object&,ET.ResourcesLoaderComponentSystem.<LoadAssetAsync>d__3<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.RouterHelper.<CreateRouterSession>d__0>(object&,ET.RouterHelper.<CreateRouterSession>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.RpcInfo.<Wait>d__7>(object&,ET.RpcInfo.<Wait>d__7&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.SessionSystem.<Call>d__3>(object&,ET.SessionSystem.<Call>d__3&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.SessionSystem.<Call>d__4>(object&,ET.SessionSystem.<Call>d__4&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,ET.WaitCoroutineLock.<Wait>d__5>(object&,ET.WaitCoroutineLock.<Wait>d__5&)
		// System.Void ET.ETAsyncTaskMethodBuilder<uint>.AwaitUnsafeOnCompleted<object,ET.RouterHelper.<Connect>d__2>(object&,ET.RouterHelper.<Connect>d__2&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.A2C_DisconnectHandler.<Run>d__0>(ET.A2C_DisconnectHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.A2NetClient_MessageHandler.<Run>d__0>(ET.A2NetClient_MessageHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.A2NetClient_RequestHandler.<Run>d__0>(ET.A2NetClient_RequestHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.AI_Attack.<Execute>d__1>(ET.AI_Attack.<Execute>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.AI_XunLuo.<Execute>d__1>(ET.AI_XunLuo.<Execute>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.AfterCreateClientScene_AddComponent.<Run>d__0>(ET.AfterCreateClientScene_AddComponent.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.AfterCreateCurrentScene_AddComponent.<Run>d__0>(ET.AfterCreateCurrentScene_AddComponent.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.AfterUnitCreate_CreateUnitView.<Run>d__0>(ET.AfterUnitCreate_CreateUnitView.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.AppStartInitFinish_CreateLoginUI.<Run>d__0>(ET.AppStartInitFinish_CreateLoginUI.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.ChangePosition_SyncGameObjectPos.<Run>d__0>(ET.ChangePosition_SyncGameObjectPos.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.ChangeRotation_SyncGameObjectRotation.<Run>d__0>(ET.ChangeRotation_SyncGameObjectRotation.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.ClientSenderComponentSystem.<DisposeAsync>d__3>(ET.ClientSenderComponentSystem.<DisposeAsync>d__3&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.ClientSenderComponentSystem.<RemoveFiberAsync>d__2>(ET.ClientSenderComponentSystem.<RemoveFiberAsync>d__2&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.ETCancellationTokenHelper.<AddCancel>d__1>(ET.ETCancellationTokenHelper.<AddCancel>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.ETCancellationTokenHelper.<TimeoutAsync>d__0>(ET.ETCancellationTokenHelper.<TimeoutAsync>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.ETCancellationTokenHelper.<TimeoutAsync>d__3>(ET.ETCancellationTokenHelper.<TimeoutAsync>d__3&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.ETCancellationTokenHelper.<TimeoutAsync>d__5>(ET.ETCancellationTokenHelper.<TimeoutAsync>d__5&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.EnterMapHelper.<EnterMapAsync>d__0>(ET.EnterMapHelper.<EnterMapAsync>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.EntryEvent1_InitShare.<Run>d__0>(ET.EntryEvent1_InitShare.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.EntryEvent3_InitClient.<Run>d__0>(ET.EntryEvent3_InitClient.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.EventSystem.<PublishAsync>d__6<object,ET.AppStartInitFinish>>(ET.EventSystem.<PublishAsync>d__6<object,ET.AppStartInitFinish>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.EventSystem.<PublishAsync>d__6<object,ET.EntryEvent1>>(ET.EventSystem.<PublishAsync>d__6<object,ET.EntryEvent1>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.EventSystem.<PublishAsync>d__6<object,ET.EntryEvent3>>(ET.EventSystem.<PublishAsync>d__6<object,ET.EntryEvent3>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.EventSystem.<PublishAsync>d__6<object,ET.LoginFinish>>(ET.EventSystem.<PublishAsync>d__6<object,ET.LoginFinish>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.EventSystem.<PublishAsync>d__6<object,ET.SceneChangeStart>>(ET.EventSystem.<PublishAsync>d__6<object,ET.SceneChangeStart>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.FiberInit_Main.<Handle>d__0>(ET.FiberInit_Main.<Handle>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.FiberInit_NetClient.<Handle>d__0>(ET.FiberInit_NetClient.<Handle>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.GameEntry.<StartAsync>d__1>(ET.GameEntry.<StartAsync>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.LoginFinish_CreateLobbyUI.<Run>d__0>(ET.LoginFinish_CreateLobbyUI.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.LoginFinish_RemoveLoginUI.<Run>d__0>(ET.LoginFinish_RemoveLoginUI.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.LoginHelper.<Login>d__0>(ET.LoginHelper.<Login>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.M2C_CreateMyUnitHandler.<Run>d__0>(ET.M2C_CreateMyUnitHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.M2C_CreateUnitsHandler.<Run>d__0>(ET.M2C_CreateUnitsHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.M2C_PathfindingResultHandler.<Run>d__0>(ET.M2C_PathfindingResultHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.M2C_RemoveUnitsHandler.<Run>d__0>(ET.M2C_RemoveUnitsHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.M2C_StartSceneChangeHandler.<Run>d__0>(ET.M2C_StartSceneChangeHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.M2C_StopHandler.<Run>d__0>(ET.M2C_StopHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.MailBoxType_UnOrderedMessageHandler.<HandleAsync>d__1>(ET.MailBoxType_UnOrderedMessageHandler.<HandleAsync>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.Main2NetClient_LoginGameHandler.<Run>d__0>(ET.Main2NetClient_LoginGameHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.Main2NetClient_LoginHandler.<Run>d__0>(ET.Main2NetClient_LoginHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.MessageDispatcher.<Handle>d__4>(ET.MessageDispatcher.<Handle>d__4&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.MessageHandler.<Handle>d__1<object,object,object>>(ET.MessageHandler.<Handle>d__1<object,object,object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.MessageHandler.<Handle>d__1<object,object>>(ET.MessageHandler.<Handle>d__1<object,object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.MessageSessionHandler.<HandleAsync>d__2<object,object>>(ET.MessageSessionHandler.<HandleAsync>d__2<object,object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.MessageSessionHandler.<HandleAsync>d__2<object>>(ET.MessageSessionHandler.<HandleAsync>d__2<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.MoveHelper.<MoveToAsync>d__1>(ET.MoveHelper.<MoveToAsync>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.NetClient2Main_SessionDisposeHandler.<Run>d__0>(ET.NetClient2Main_SessionDisposeHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.NumericChangeEvent_NotifyWatcher.<Run>d__0>(ET.NumericChangeEvent_NotifyWatcher.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.OperaComponentSystem.<Test1>d__2>(ET.OperaComponentSystem.<Test1>d__2&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.OperaComponentSystem.<Test2>d__3>(ET.OperaComponentSystem.<Test2>d__3&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.OperaComponentSystem.<TestCancelAfter>d__4>(ET.OperaComponentSystem.<TestCancelAfter>d__4&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.PingComponentSystem.<PingAsync>d__2>(ET.PingComponentSystem.<PingAsync>d__2&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.ProcessInnerSenderSystem.<>c__DisplayClass10_0.<<Call>g__Timeout|0>d>(ET.ProcessInnerSenderSystem.<>c__DisplayClass10_0.<<Call>g__Timeout|0>d&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.ResourcesLoaderComponentSystem.<LoadSceneAsync>d__5>(ET.ResourcesLoaderComponentSystem.<LoadSceneAsync>d__5&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.RouterAddressComponentSystem.<GetAllRouter>d__1>(ET.RouterAddressComponentSystem.<GetAllRouter>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.RouterAddressComponentSystem.<RefreshRouterAsync>d__2>(ET.RouterAddressComponentSystem.<RefreshRouterAsync>d__2&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.RouterCheckComponentSystem.<CheckAsync>d__1>(ET.RouterCheckComponentSystem.<CheckAsync>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.SceneChangeFinishEvent_CreateUIHelp.<Run>d__0>(ET.SceneChangeFinishEvent_CreateUIHelp.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.SceneChangeHelper.<SceneChangeTo>d__0>(ET.SceneChangeHelper.<SceneChangeTo>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.SceneChangeStart_AddComponent.<Run>d__0>(ET.SceneChangeStart_AddComponent.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.TimerComponentSystem.<WaitAsync>d__10>(ET.TimerComponentSystem.<WaitAsync>d__10&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.TimerComponentSystem.<WaitFrameAsync>d__9>(ET.TimerComponentSystem.<WaitFrameAsync>d__9&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<ET.TimerComponentSystem.<WaitTillAsync>d__8>(ET.TimerComponentSystem.<WaitTillAsync>d__8&)
		// System.Void ET.ETAsyncTaskMethodBuilder<ET.Wait_CreateMyUnit>.Start<ET.ObjectWaitSystem.<Wait>d__2<ET.Wait_CreateMyUnit>>(ET.ObjectWaitSystem.<Wait>d__2<ET.Wait_CreateMyUnit>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<ET.Wait_SceneChangeFinish>.Start<ET.ObjectWaitSystem.<Wait>d__2<ET.Wait_SceneChangeFinish>>(ET.ObjectWaitSystem.<Wait>d__2<ET.Wait_SceneChangeFinish>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<ET.Wait_UnitStop>.Start<ET.ObjectWaitSystem.<Wait>d__2<ET.Wait_UnitStop>>(ET.ObjectWaitSystem.<Wait>d__2<ET.Wait_UnitStop>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<System.ValueTuple<uint,object>>.Start<ET.RouterHelper.<GetRouterAddress>d__1>(ET.RouterHelper.<GetRouterAddress>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder<byte>.Start<ET.MoveComponentSystem.<MoveToAsync>d__4>(ET.MoveComponentSystem.<MoveToAsync>d__4&)
		// System.Void ET.ETAsyncTaskMethodBuilder<int>.Start<ET.MoveHelper.<MoveToAsync>d__0>(ET.MoveHelper.<MoveToAsync>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.ClientSenderComponentSystem.<Call>d__7>(ET.ClientSenderComponentSystem.<Call>d__7&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.ClientSenderComponentSystem.<LoginAsync>d__4>(ET.ClientSenderComponentSystem.<LoginAsync>d__4&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.ClientSenderComponentSystem.<LoginGameAsync>d__5>(ET.ClientSenderComponentSystem.<LoginGameAsync>d__5&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.CoroutineLockComponentSystem.<Wait>d__3>(ET.CoroutineLockComponentSystem.<Wait>d__3&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.CoroutineLockQueueSystem.<Wait>d__2>(ET.CoroutineLockQueueSystem.<Wait>d__2&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.CoroutineLockQueueTypeSystem.<Wait>d__4>(ET.CoroutineLockQueueTypeSystem.<Wait>d__4&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.ETCancellationTokenHelper.<AddCancel>d__2<object>>(ET.ETCancellationTokenHelper.<AddCancel>d__2<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.ETCancellationTokenHelper.<TimeoutAsync>d__4<object>>(ET.ETCancellationTokenHelper.<TimeoutAsync>d__4<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.ETCancellationTokenHelper.<TimeoutAsync>d__6<object>>(ET.ETCancellationTokenHelper.<TimeoutAsync>d__6<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.ETTaskHelper.<GetContextAsync>d__0<object>>(ET.ETTaskHelper.<GetContextAsync>d__0<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.MessageSenderStruct.<Wait>d__13>(ET.MessageSenderStruct.<Wait>d__13&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.ObjectWaitSystem.<Wait>d__2<object>>(ET.ObjectWaitSystem.<Wait>d__2<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.ProcessInnerSenderSystem.<Call>d__10>(ET.ProcessInnerSenderSystem.<Call>d__10&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.ResourcesLoaderComponentSystem.<LoadAllAssetsAsync>d__4<object>>(ET.ResourcesLoaderComponentSystem.<LoadAllAssetsAsync>d__4<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.ResourcesLoaderComponentSystem.<LoadAssetAsync>d__3<object>>(ET.ResourcesLoaderComponentSystem.<LoadAssetAsync>d__3<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.RouterHelper.<CreateRouterSession>d__0>(ET.RouterHelper.<CreateRouterSession>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.RpcInfo.<Wait>d__7>(ET.RpcInfo.<Wait>d__7&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.SessionSystem.<Call>d__3>(ET.SessionSystem.<Call>d__3&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.SessionSystem.<Call>d__4>(ET.SessionSystem.<Call>d__4&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<ET.WaitCoroutineLock.<Wait>d__5>(ET.WaitCoroutineLock.<Wait>d__5&)
		// System.Void ET.ETAsyncTaskMethodBuilder<uint>.Start<ET.RouterHelper.<Connect>d__2>(ET.RouterHelper.<Connect>d__2&)
		// object ET.Entity.AddChild<object,long,long,int>(long,long,int,bool)
		// object ET.Entity.AddChildWithId<object,int>(long,int,bool)
		// object ET.Entity.AddChildWithId<object,long>(long,long,bool)
		// object ET.Entity.AddChildWithId<object,object>(long,object,bool)
		// object ET.Entity.AddChildWithId<object>(long,bool)
		// object ET.Entity.AddComponent<object,int>(int,bool)
		// object ET.Entity.AddComponent<object,object>(object,bool)
		// object ET.Entity.AddComponent<object>(bool)
		// object ET.Entity.AddComponentWithId<object,int>(long,int,bool)
		// object ET.Entity.AddComponentWithId<object,object>(long,object,bool)
		// object ET.Entity.AddComponentWithId<object>(long,bool)
		// object ET.Entity.CreateAndAddChild<object,int>(bool,long,System.Action<object,int>,int)
		// object ET.Entity.CreateAndAddChild<object,long,long,int>(bool,long,System.Action<object,long,long,int>,long,long,int)
		// object ET.Entity.CreateAndAddChild<object,long>(bool,long,System.Action<object,long>,long)
		// object ET.Entity.CreateAndAddChild<object,object>(bool,long,System.Action<object,object>,object)
		// object ET.Entity.CreateAndAddChild<object>(bool,long,System.Action<object>)
		// object ET.Entity.CreateAndAddComponent<object,int>(long,bool,System.Action<object,int>,int)
		// object ET.Entity.CreateAndAddComponent<object,object>(long,bool,System.Action<object,object>,object)
		// object ET.Entity.CreateAndAddComponent<object>(long,bool,System.Action<object>)
		// object ET.Entity.GetChild<object>(long)
		// object ET.Entity.GetComponent<object>()
		// object ET.Entity.GetParent<object>()
		// System.Void ET.Entity.RemoveComponent<object>()
		// System.Void ET.Entity.ValidateComponentNotExists<object>()
		// object ET.EntityObjectPool.CreateEntity<object>(bool)
		// object ET.EntityObjectPool.GetEntity<object>(long,bool,int)
		// System.Void ET.EntitySystemSingleton.RegisterEntitySystem<object>()
		// System.Void ET.EventSystem.Invoke<ET.MailBoxInvoker>(long,ET.MailBoxInvoker)
		// System.Void ET.EventSystem.Invoke<ET.NetComponentOnRead>(long,ET.NetComponentOnRead)
		// System.Void ET.EventSystem.Invoke<ET.TimerCallback>(long,ET.TimerCallback)
		// System.Void ET.EventSystem.Publish<object,ET.AfterCreateCurrentScene>(object,ET.AfterCreateCurrentScene)
		// System.Void ET.EventSystem.Publish<object,ET.AfterUnitCreate>(object,ET.AfterUnitCreate)
		// System.Void ET.EventSystem.Publish<object,ET.ChangePosition>(object,ET.ChangePosition)
		// System.Void ET.EventSystem.Publish<object,ET.ChangeRotation>(object,ET.ChangeRotation)
		// System.Void ET.EventSystem.Publish<object,ET.EnterMapFinish>(object,ET.EnterMapFinish)
		// System.Void ET.EventSystem.Publish<object,ET.MoveStart>(object,ET.MoveStart)
		// System.Void ET.EventSystem.Publish<object,ET.MoveStop>(object,ET.MoveStop)
		// System.Void ET.EventSystem.Publish<object,ET.NumbericChange>(object,ET.NumbericChange)
		// System.Void ET.EventSystem.Publish<object,ET.SceneChangeFinish>(object,ET.SceneChangeFinish)
		// System.Void ET.EventSystem.Publish<object,ET.SceneChangeStart>(object,ET.SceneChangeStart)
		// ET.ETTask ET.EventSystem.PublishAsync<object,ET.AppStartInitFinish>(object,ET.AppStartInitFinish)
		// ET.ETTask ET.EventSystem.PublishAsync<object,ET.EntryEvent1>(object,ET.EntryEvent1)
		// ET.ETTask ET.EventSystem.PublishAsync<object,ET.EntryEvent3>(object,ET.EntryEvent3)
		// ET.ETTask ET.EventSystem.PublishAsync<object,ET.LoginFinish>(object,ET.LoginFinish)
		// ET.ETTask ET.EventSystem.PublishAsync<object,ET.SceneChangeStart>(object,ET.SceneChangeStart)
		// System.Void ET.EventSystem.RegisterEvent<object>(int)
		// System.Void ET.EventSystem.RegisterInvoke<object>(long)
		// System.Void ET.RandomGenerator.BreakRank<object>(System.Collections.Generic.List<object>)
		// object ET.World.AddSingleton<object,object>(object)
		// object ET.World.AddSingleton<object>()
		// string Luban.StringUtil.CollectionToString<int>(System.Collections.Generic.IEnumerable<int>)
		// System.Void Nino.Core.CachedDeserializer<object>.AddSubTypeDeserializer<object>(int,Nino.Core.DeserializeDelegate<object>,Nino.Core.DeserializeDelegateRef<object>)
		// System.Void Nino.Core.CachedSerializer<object>.AddSubTypeSerializer<object>(Nino.Core.SerializeDelegate<object>)
		// System.Void Nino.Core.NinoDeserializer.Deserialize<object>(object&,Nino.Core.Reader&)
		// object Nino.Core.NinoDeserializer.Deserialize<object>(System.ReadOnlySpan<byte>)
		// System.Void Nino.Core.NinoSerializer.Serialize<object>(object,Nino.Core.INinoBufferWriter)
		// System.Void Nino.Core.NinoSerializer.Serialize<object>(object,Nino.Core.Writer&)
		// Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int> Nino.Core.NinoTuple.Create<Unity.Mathematics.quaternion,int>(Unity.Mathematics.quaternion,int)
		// Nino.Core.NinoTuple<int,ET.ActorId> Nino.Core.NinoTuple.Create<int,ET.ActorId>(int,ET.ActorId)
		// Nino.Core.NinoTuple<int,Unity.Mathematics.float3> Nino.Core.NinoTuple.Create<int,Unity.Mathematics.float3>(int,Unity.Mathematics.float3)
		// Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId> Nino.Core.NinoTuple.Create<int,int,long,ET.ActorId,ET.ActorId>(int,int,long,ET.ActorId,ET.ActorId)
		// Nino.Core.NinoTuple<int,int,long,ET.ActorId,int> Nino.Core.NinoTuple.Create<int,int,long,ET.ActorId,int>(int,int,long,ET.ActorId,int)
		// Nino.Core.NinoTuple<int,int,long,ET.ActorId> Nino.Core.NinoTuple.Create<int,int,long,ET.ActorId>(int,int,long,ET.ActorId)
		// Nino.Core.NinoTuple<int,int,long> Nino.Core.NinoTuple.Create<int,int,long>(int,int,long)
		// Nino.Core.NinoTuple<int,int> Nino.Core.NinoTuple.Create<int,int>(int,int)
		// Nino.Core.NinoTuple<int,long,ET.ActorId> Nino.Core.NinoTuple.Create<int,long,ET.ActorId>(int,long,ET.ActorId)
		// Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion> Nino.Core.NinoTuple.Create<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>(int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion)
		// Nino.Core.NinoTuple<int,long,long> Nino.Core.NinoTuple.Create<int,long,long>(int,long,long)
		// Nino.Core.NinoTuple<int,long> Nino.Core.NinoTuple.Create<int,long>(int,long)
		// Nino.Core.NinoTuple<long,Unity.Mathematics.float3> Nino.Core.NinoTuple.Create<long,Unity.Mathematics.float3>(long,Unity.Mathematics.float3)
		// Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3> Nino.Core.NinoTuple.Create<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>(long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3)
		// Nino.Core.NinoTuple<long,long> Nino.Core.NinoTuple.Create<long,long>(long,long)
		// System.Void Nino.Core.NinoTypeMetadata.RecordSubTypeDeserializer<object,object>(int,Nino.Core.DeserializeDelegate<object>,Nino.Core.DeserializeDelegateRef<object>)
		// System.Void Nino.Core.NinoTypeMetadata.RecordSubTypeSerializer<object,object>(Nino.Core.SerializeDelegate<object>)
		// System.Void Nino.Core.NinoTypeMetadata.RegisterDeserializer<ET.ActorId>(int,Nino.Core.DeserializeDelegate<ET.ActorId>,Nino.Core.DeserializeDelegateRef<ET.ActorId>,Nino.Core.DeserializeDelegate<ET.ActorId>,Nino.Core.DeserializeDelegateRef<ET.ActorId>,bool)
		// System.Void Nino.Core.NinoTypeMetadata.RegisterDeserializer<ET.Address>(int,Nino.Core.DeserializeDelegate<ET.Address>,Nino.Core.DeserializeDelegateRef<ET.Address>,Nino.Core.DeserializeDelegate<ET.Address>,Nino.Core.DeserializeDelegateRef<ET.Address>,bool)
		// System.Void Nino.Core.NinoTypeMetadata.RegisterDeserializer<System.ValueTuple<ET.ActorId,object>>(int,Nino.Core.DeserializeDelegate<System.ValueTuple<ET.ActorId,object>>,Nino.Core.DeserializeDelegateRef<System.ValueTuple<ET.ActorId,object>>,Nino.Core.DeserializeDelegate<System.ValueTuple<ET.ActorId,object>>,Nino.Core.DeserializeDelegateRef<System.ValueTuple<ET.ActorId,object>>,bool)
		// System.Void Nino.Core.NinoTypeMetadata.RegisterDeserializer<System.ValueTuple<long,long,int>>(int,Nino.Core.DeserializeDelegate<System.ValueTuple<long,long,int>>,Nino.Core.DeserializeDelegateRef<System.ValueTuple<long,long,int>>,Nino.Core.DeserializeDelegate<System.ValueTuple<long,long,int>>,Nino.Core.DeserializeDelegateRef<System.ValueTuple<long,long,int>>,bool)
		// System.Void Nino.Core.NinoTypeMetadata.RegisterDeserializer<object>(int,Nino.Core.DeserializeDelegate<object>,Nino.Core.DeserializeDelegateRef<object>,Nino.Core.DeserializeDelegate<object>,Nino.Core.DeserializeDelegateRef<object>,bool)
		// System.Void Nino.Core.NinoTypeMetadata.RegisterSerializer<ET.ActorId>(Nino.Core.SerializeDelegate<ET.ActorId>,bool)
		// System.Void Nino.Core.NinoTypeMetadata.RegisterSerializer<ET.Address>(Nino.Core.SerializeDelegate<ET.Address>,bool)
		// System.Void Nino.Core.NinoTypeMetadata.RegisterSerializer<System.ValueTuple<ET.ActorId,object>>(Nino.Core.SerializeDelegate<System.ValueTuple<ET.ActorId,object>>,bool)
		// System.Void Nino.Core.NinoTypeMetadata.RegisterSerializer<System.ValueTuple<long,long,int>>(Nino.Core.SerializeDelegate<System.ValueTuple<long,long,int>>,bool)
		// System.Void Nino.Core.NinoTypeMetadata.RegisterSerializer<object>(Nino.Core.SerializeDelegate<object>,bool)
		// System.Void Nino.Core.Reader.Peak<int>(int&)
		// System.Void Nino.Core.Reader.Read<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>>(Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>&)
		// System.Void Nino.Core.Reader.Read<Nino.Core.NinoTuple<int,ET.ActorId>>(Nino.Core.NinoTuple<int,ET.ActorId>&)
		// System.Void Nino.Core.Reader.Read<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<int,Unity.Mathematics.float3>&)
		// System.Void Nino.Core.Reader.Read<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>&)
		// System.Void Nino.Core.Reader.Read<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>&)
		// System.Void Nino.Core.Reader.Read<Nino.Core.NinoTuple<int,int,long,ET.ActorId>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId>&)
		// System.Void Nino.Core.Reader.Read<Nino.Core.NinoTuple<int,int,long>>(Nino.Core.NinoTuple<int,int,long>&)
		// System.Void Nino.Core.Reader.Read<Nino.Core.NinoTuple<int,int>>(Nino.Core.NinoTuple<int,int>&)
		// System.Void Nino.Core.Reader.Read<Nino.Core.NinoTuple<int,long,ET.ActorId>>(Nino.Core.NinoTuple<int,long,ET.ActorId>&)
		// System.Void Nino.Core.Reader.Read<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>>(Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>&)
		// System.Void Nino.Core.Reader.Read<Nino.Core.NinoTuple<int,long,long>>(Nino.Core.NinoTuple<int,long,long>&)
		// System.Void Nino.Core.Reader.Read<Nino.Core.NinoTuple<int,long>>(Nino.Core.NinoTuple<int,long>&)
		// System.Void Nino.Core.Reader.Read<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<long,Unity.Mathematics.float3>&)
		// System.Void Nino.Core.Reader.Read<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>&)
		// System.Void Nino.Core.Reader.Read<Nino.Core.NinoTuple<long,long>>(Nino.Core.NinoTuple<long,long>&)
		// System.Void Nino.Core.Reader.Read<System.ValueTuple<long,long,int>>(System.ValueTuple<long,long,int>&)
		// System.Void Nino.Core.Reader.Read<Unity.Mathematics.float3>(System.Collections.Generic.List<Unity.Mathematics.float3>&)
		// System.Void Nino.Core.Reader.Read<Unity.Mathematics.float3>(Unity.Mathematics.float3[]&)
		// System.Void Nino.Core.Reader.Read<byte>(byte[]&)
		// System.Void Nino.Core.Reader.Read<int>(System.Collections.Generic.List<int>&)
		// System.Void Nino.Core.Reader.Read<int>(int&)
		// System.Void Nino.Core.Reader.Read<int>(int[]&)
		// System.Void Nino.Core.Reader.Read<long>(System.Collections.Generic.List<long>&)
		// System.Void Nino.Core.Reader.Read<long>(long[]&)
		// System.Void Nino.Core.Reader.ReadRef<Unity.Mathematics.float3>(System.Collections.Generic.List<Unity.Mathematics.float3>&)
		// System.Void Nino.Core.Reader.ReadRef<Unity.Mathematics.float3>(Unity.Mathematics.float3[]&)
		// System.Void Nino.Core.Reader.ReadRef<byte>(byte[]&)
		// System.Void Nino.Core.Reader.ReadRef<int>(System.Collections.Generic.List<int>&)
		// System.Void Nino.Core.Reader.ReadRef<long>(System.Collections.Generic.List<long>&)
		// System.Void Nino.Core.Reader.UnsafeRead<ET.ActorId>(ET.ActorId&)
		// System.Void Nino.Core.Reader.UnsafeRead<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>>(Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>&)
		// System.Void Nino.Core.Reader.UnsafeRead<Nino.Core.NinoTuple<int,ET.ActorId>>(Nino.Core.NinoTuple<int,ET.ActorId>&)
		// System.Void Nino.Core.Reader.UnsafeRead<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<int,Unity.Mathematics.float3>&)
		// System.Void Nino.Core.Reader.UnsafeRead<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>&)
		// System.Void Nino.Core.Reader.UnsafeRead<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>&)
		// System.Void Nino.Core.Reader.UnsafeRead<Nino.Core.NinoTuple<int,int,long,ET.ActorId>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId>&)
		// System.Void Nino.Core.Reader.UnsafeRead<Nino.Core.NinoTuple<int,int,long>>(Nino.Core.NinoTuple<int,int,long>&)
		// System.Void Nino.Core.Reader.UnsafeRead<Nino.Core.NinoTuple<int,int>>(Nino.Core.NinoTuple<int,int>&)
		// System.Void Nino.Core.Reader.UnsafeRead<Nino.Core.NinoTuple<int,long,ET.ActorId>>(Nino.Core.NinoTuple<int,long,ET.ActorId>&)
		// System.Void Nino.Core.Reader.UnsafeRead<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>>(Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>&)
		// System.Void Nino.Core.Reader.UnsafeRead<Nino.Core.NinoTuple<int,long,long>>(Nino.Core.NinoTuple<int,long,long>&)
		// System.Void Nino.Core.Reader.UnsafeRead<Nino.Core.NinoTuple<int,long>>(Nino.Core.NinoTuple<int,long>&)
		// System.Void Nino.Core.Reader.UnsafeRead<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<long,Unity.Mathematics.float3>&)
		// System.Void Nino.Core.Reader.UnsafeRead<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>&)
		// System.Void Nino.Core.Reader.UnsafeRead<Nino.Core.NinoTuple<long,long>>(Nino.Core.NinoTuple<long,long>&)
		// System.Void Nino.Core.Reader.UnsafeRead<System.Collections.Generic.KeyValuePair<int,long>>(System.Collections.Generic.KeyValuePair<int,long>&)
		// System.Void Nino.Core.Reader.UnsafeRead<System.ValueTuple<long,long,int>>(System.ValueTuple<long,long,int>&)
		// System.Void Nino.Core.Reader.UnsafeRead<int>(int&)
		// System.Void Nino.Core.Reader.UnsafeRead<long>(long&)
		// System.Void Nino.Core.Reader.UnsafeRead<uint>(uint&)
		// System.Void Nino.Core.Writer.UnsafeWrite<ET.ActorId>(ET.ActorId)
		// System.Void Nino.Core.Writer.UnsafeWrite<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>>(Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>)
		// System.Void Nino.Core.Writer.UnsafeWrite<Nino.Core.NinoTuple<int,ET.ActorId>>(Nino.Core.NinoTuple<int,ET.ActorId>)
		// System.Void Nino.Core.Writer.UnsafeWrite<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<int,Unity.Mathematics.float3>)
		// System.Void Nino.Core.Writer.UnsafeWrite<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>)
		// System.Void Nino.Core.Writer.UnsafeWrite<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>)
		// System.Void Nino.Core.Writer.UnsafeWrite<Nino.Core.NinoTuple<int,int,long,ET.ActorId>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId>)
		// System.Void Nino.Core.Writer.UnsafeWrite<Nino.Core.NinoTuple<int,int,long>>(Nino.Core.NinoTuple<int,int,long>)
		// System.Void Nino.Core.Writer.UnsafeWrite<Nino.Core.NinoTuple<int,int>>(Nino.Core.NinoTuple<int,int>)
		// System.Void Nino.Core.Writer.UnsafeWrite<Nino.Core.NinoTuple<int,long,ET.ActorId>>(Nino.Core.NinoTuple<int,long,ET.ActorId>)
		// System.Void Nino.Core.Writer.UnsafeWrite<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>>(Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>)
		// System.Void Nino.Core.Writer.UnsafeWrite<Nino.Core.NinoTuple<int,long,long>>(Nino.Core.NinoTuple<int,long,long>)
		// System.Void Nino.Core.Writer.UnsafeWrite<Nino.Core.NinoTuple<int,long>>(Nino.Core.NinoTuple<int,long>)
		// System.Void Nino.Core.Writer.UnsafeWrite<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<long,Unity.Mathematics.float3>)
		// System.Void Nino.Core.Writer.UnsafeWrite<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>)
		// System.Void Nino.Core.Writer.UnsafeWrite<Nino.Core.NinoTuple<long,long>>(Nino.Core.NinoTuple<long,long>)
		// System.Void Nino.Core.Writer.UnsafeWrite<System.Collections.Generic.KeyValuePair<int,long>>(System.Collections.Generic.KeyValuePair<int,long>)
		// System.Void Nino.Core.Writer.UnsafeWrite<System.ValueTuple<long,long,int>>(System.ValueTuple<long,long,int>)
		// System.Void Nino.Core.Writer.UnsafeWrite<int>(int)
		// System.Void Nino.Core.Writer.UnsafeWrite<long>(long)
		// System.Void Nino.Core.Writer.UnsafeWrite<uint>(uint)
		// System.Void Nino.Core.Writer.Write<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>>(Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>)
		// System.Void Nino.Core.Writer.Write<Nino.Core.NinoTuple<int,ET.ActorId>>(Nino.Core.NinoTuple<int,ET.ActorId>)
		// System.Void Nino.Core.Writer.Write<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<int,Unity.Mathematics.float3>)
		// System.Void Nino.Core.Writer.Write<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>)
		// System.Void Nino.Core.Writer.Write<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>)
		// System.Void Nino.Core.Writer.Write<Nino.Core.NinoTuple<int,int,long,ET.ActorId>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId>)
		// System.Void Nino.Core.Writer.Write<Nino.Core.NinoTuple<int,int,long>>(Nino.Core.NinoTuple<int,int,long>)
		// System.Void Nino.Core.Writer.Write<Nino.Core.NinoTuple<int,int>>(Nino.Core.NinoTuple<int,int>)
		// System.Void Nino.Core.Writer.Write<Nino.Core.NinoTuple<int,long,ET.ActorId>>(Nino.Core.NinoTuple<int,long,ET.ActorId>)
		// System.Void Nino.Core.Writer.Write<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>>(Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>)
		// System.Void Nino.Core.Writer.Write<Nino.Core.NinoTuple<int,long,long>>(Nino.Core.NinoTuple<int,long,long>)
		// System.Void Nino.Core.Writer.Write<Nino.Core.NinoTuple<int,long>>(Nino.Core.NinoTuple<int,long>)
		// System.Void Nino.Core.Writer.Write<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<long,Unity.Mathematics.float3>)
		// System.Void Nino.Core.Writer.Write<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>)
		// System.Void Nino.Core.Writer.Write<Nino.Core.NinoTuple<long,long>>(Nino.Core.NinoTuple<long,long>)
		// System.Void Nino.Core.Writer.Write<System.ValueTuple<long,long,int>>(System.ValueTuple<long,long,int>)
		// System.Void Nino.Core.Writer.Write<Unity.Mathematics.float3>(System.Collections.Generic.List<Unity.Mathematics.float3>)
		// System.Void Nino.Core.Writer.Write<Unity.Mathematics.float3>(System.Span<Unity.Mathematics.float3>)
		// System.Void Nino.Core.Writer.Write<Unity.Mathematics.float3>(Unity.Mathematics.float3[])
		// System.Void Nino.Core.Writer.Write<byte>(System.Span<byte>)
		// System.Void Nino.Core.Writer.Write<byte>(byte[])
		// System.Void Nino.Core.Writer.Write<int>(System.Collections.Generic.List<int>)
		// System.Void Nino.Core.Writer.Write<int>(System.Span<int>)
		// System.Void Nino.Core.Writer.Write<int>(int)
		// System.Void Nino.Core.Writer.Write<long>(System.Collections.Generic.List<long>)
		// System.Void Nino.Core.Writer.Write<long>(System.Span<long>)
		// System.Void Nino.Core.Writer.Write<long>(long)
		// System.Void Nino.Core.Writer.Write<uint>(uint)
		// object System.Activator.CreateInstance<object>()
		// Unity.Mathematics.float3[] System.Array.Empty<Unity.Mathematics.float3>()
		// byte[] System.Array.Empty<byte>()
		// int[] System.Array.Empty<int>()
		// long[] System.Array.Empty<long>()
		// System.Void System.Array.Resize<Unity.Mathematics.float3>(Unity.Mathematics.float3[]&,int)
		// System.Void System.Array.Resize<int>(int[]&,int)
		// System.Void System.Array.Resize<long>(long[]&,int)
		// System.Void System.Array.Resize<object>(object[]&,int)
		// bool System.Collections.Generic.CollectionExtensions.Remove<long,object>(System.Collections.Generic.IDictionary<long,object>,long,object&)
		// ET.RpcInfo[] System.Linq.Enumerable.ToArray<ET.RpcInfo>(System.Collections.Generic.IEnumerable<ET.RpcInfo>)
		// object[] System.Linq.Enumerable.ToArray<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Span<Unity.Mathematics.float3> System.MemoryExtensions.AsSpan<Unity.Mathematics.float3>(Unity.Mathematics.float3[])
		// System.Span<Unity.Mathematics.float3> System.MemoryExtensions.AsSpan<Unity.Mathematics.float3>(Unity.Mathematics.float3[],int,int)
		// System.Span<byte> System.MemoryExtensions.AsSpan<byte>(byte[])
		// System.Span<byte> System.MemoryExtensions.AsSpan<byte>(byte[],int,int)
		// System.Span<int> System.MemoryExtensions.AsSpan<int>(int[])
		// System.Span<int> System.MemoryExtensions.AsSpan<int>(int[],int,int)
		// System.Span<long> System.MemoryExtensions.AsSpan<long>(long[])
		// System.Span<long> System.MemoryExtensions.AsSpan<long>(long[],int,int)
		// System.Span<object> System.MemoryExtensions.AsSpan<object>(object[])
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<object,ET.GameEntry.<Awake>d__0>(object&,ET.GameEntry.<Awake>d__0&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<object,GameUI.UILobbyPanel.<<OnInitUI>b__1_0>d>(object&,GameUI.UILobbyPanel.<<OnInitUI>b__1_0>d&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<ET.GameEntry.<Awake>d__0>(ET.GameEntry.<Awake>d__0&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<GameUI.UILobbyPanel.<<OnInitUI>b__1_0>d>(GameUI.UILobbyPanel.<<OnInitUI>b__1_0>d&)
		// bool System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<Unity.Mathematics.float3>()
		// bool System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<byte>()
		// bool System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<int>()
		// bool System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<long>()
		// bool System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<uint>()
		// Nino.Core.Internal.DictionaryView.Entry<int,long>& System.Runtime.CompilerServices.Unsafe.Add<Nino.Core.Internal.DictionaryView.Entry<int,long>>(Nino.Core.Internal.DictionaryView.Entry<int,long>&,int)
		// object& System.Runtime.CompilerServices.Unsafe.Add<object>(object&,int)
		// System.Collections.Generic.KeyValuePair<int,long>& System.Runtime.CompilerServices.Unsafe.As<int,System.Collections.Generic.KeyValuePair<int,long>>(int&)
		// System.Threading.Volatile.VolatileObject& System.Runtime.CompilerServices.Unsafe.As<object,System.Threading.Volatile.VolatileObject>(object&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<ET.ActorId,byte>(ET.ActorId&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>,byte>(Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,ET.ActorId>,byte>(Nino.Core.NinoTuple<int,ET.ActorId>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>,byte>(Nino.Core.NinoTuple<int,Unity.Mathematics.float3>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>,byte>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>,byte>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,int,long,ET.ActorId>,byte>(Nino.Core.NinoTuple<int,int,long,ET.ActorId>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,int,long>,byte>(Nino.Core.NinoTuple<int,int,long>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,int>,byte>(Nino.Core.NinoTuple<int,int>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,long,ET.ActorId>,byte>(Nino.Core.NinoTuple<int,long,ET.ActorId>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>,byte>(Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,long,long>,byte>(Nino.Core.NinoTuple<int,long,long>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,long>,byte>(Nino.Core.NinoTuple<int,long>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>,byte>(Nino.Core.NinoTuple<long,Unity.Mathematics.float3>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>,byte>(Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<long,long>,byte>(Nino.Core.NinoTuple<long,long>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<System.Collections.Generic.KeyValuePair<int,long>,byte>(System.Collections.Generic.KeyValuePair<int,long>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<System.ValueTuple<long,long,int>,byte>(System.ValueTuple<long,long,int>&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<Unity.Mathematics.float3,byte>(Unity.Mathematics.float3&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<byte,byte>(byte&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<int,byte>(int&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<int,byte>(int&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<long,byte>(long&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<long,byte>(long&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<uint,byte>(uint&)
		// byte& System.Runtime.CompilerServices.Unsafe.As<uint,byte>(uint&)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<ET.ActorId,uint>(ET.ActorId&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>,uint>(Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,ET.ActorId>,uint>(Nino.Core.NinoTuple<int,ET.ActorId>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>,uint>(Nino.Core.NinoTuple<int,Unity.Mathematics.float3>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>,uint>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>,uint>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,int,long,ET.ActorId>,uint>(Nino.Core.NinoTuple<int,int,long,ET.ActorId>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,int,long>,uint>(Nino.Core.NinoTuple<int,int,long>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,int>,uint>(Nino.Core.NinoTuple<int,int>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,long,ET.ActorId>,uint>(Nino.Core.NinoTuple<int,long,ET.ActorId>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>,uint>(Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,long,long>,uint>(Nino.Core.NinoTuple<int,long,long>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,long>,uint>(Nino.Core.NinoTuple<int,long>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>,uint>(Nino.Core.NinoTuple<long,Unity.Mathematics.float3>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>,uint>(Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<long,long>,uint>(Nino.Core.NinoTuple<long,long>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<System.Collections.Generic.KeyValuePair<int,long>,uint>(System.Collections.Generic.KeyValuePair<int,long>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<System.ValueTuple<long,long,int>,uint>(System.ValueTuple<long,long,int>&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<int,uint>(int&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<long,uint>(long&)
		// uint& System.Runtime.CompilerServices.Unsafe.As<uint,uint>(uint&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<ET.ActorId,ushort>(ET.ActorId&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>,ushort>(Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,ET.ActorId>,ushort>(Nino.Core.NinoTuple<int,ET.ActorId>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>,ushort>(Nino.Core.NinoTuple<int,Unity.Mathematics.float3>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>,ushort>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>,ushort>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,int,long,ET.ActorId>,ushort>(Nino.Core.NinoTuple<int,int,long,ET.ActorId>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,int,long>,ushort>(Nino.Core.NinoTuple<int,int,long>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,int>,ushort>(Nino.Core.NinoTuple<int,int>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,long,ET.ActorId>,ushort>(Nino.Core.NinoTuple<int,long,ET.ActorId>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>,ushort>(Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,long,long>,ushort>(Nino.Core.NinoTuple<int,long,long>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<int,long>,ushort>(Nino.Core.NinoTuple<int,long>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>,ushort>(Nino.Core.NinoTuple<long,Unity.Mathematics.float3>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>,ushort>(Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<Nino.Core.NinoTuple<long,long>,ushort>(Nino.Core.NinoTuple<long,long>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<System.Collections.Generic.KeyValuePair<int,long>,ushort>(System.Collections.Generic.KeyValuePair<int,long>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<System.ValueTuple<long,long,int>,ushort>(System.ValueTuple<long,long,int>&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<int,ushort>(int&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<long,ushort>(long&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<uint,ushort>(uint&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<ET.ActorId>(ET.ActorId&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>>(Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<Nino.Core.NinoTuple<int,ET.ActorId>>(Nino.Core.NinoTuple<int,ET.ActorId>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<int,Unity.Mathematics.float3>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<Nino.Core.NinoTuple<int,int,long,ET.ActorId>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<Nino.Core.NinoTuple<int,int,long>>(Nino.Core.NinoTuple<int,int,long>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<Nino.Core.NinoTuple<int,int>>(Nino.Core.NinoTuple<int,int>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<Nino.Core.NinoTuple<int,long,ET.ActorId>>(Nino.Core.NinoTuple<int,long,ET.ActorId>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>>(Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<Nino.Core.NinoTuple<int,long,long>>(Nino.Core.NinoTuple<int,long,long>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<Nino.Core.NinoTuple<int,long>>(Nino.Core.NinoTuple<int,long>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<long,Unity.Mathematics.float3>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<Nino.Core.NinoTuple<long,long>>(Nino.Core.NinoTuple<long,long>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<System.Collections.Generic.KeyValuePair<int,long>>(System.Collections.Generic.KeyValuePair<int,long>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<System.ValueTuple<long,long,int>>(System.ValueTuple<long,long,int>&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<int>(int&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<long>(long&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<object>(object&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<uint>(uint&)
		// bool System.Runtime.CompilerServices.Unsafe.IsAddressLessThan<object>(object&,object&)
		// ET.ActorId System.Runtime.CompilerServices.Unsafe.ReadUnaligned<ET.ActorId>(byte&)
		// Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>>(byte&)
		// Nino.Core.NinoTuple<int,ET.ActorId> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Nino.Core.NinoTuple<int,ET.ActorId>>(byte&)
		// Nino.Core.NinoTuple<int,Unity.Mathematics.float3> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>>(byte&)
		// Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>>(byte&)
		// Nino.Core.NinoTuple<int,int,long,ET.ActorId,int> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>>(byte&)
		// Nino.Core.NinoTuple<int,int,long,ET.ActorId> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Nino.Core.NinoTuple<int,int,long,ET.ActorId>>(byte&)
		// Nino.Core.NinoTuple<int,int,long> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Nino.Core.NinoTuple<int,int,long>>(byte&)
		// Nino.Core.NinoTuple<int,int> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Nino.Core.NinoTuple<int,int>>(byte&)
		// Nino.Core.NinoTuple<int,long,ET.ActorId> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Nino.Core.NinoTuple<int,long,ET.ActorId>>(byte&)
		// Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>>(byte&)
		// Nino.Core.NinoTuple<int,long,long> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Nino.Core.NinoTuple<int,long,long>>(byte&)
		// Nino.Core.NinoTuple<int,long> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Nino.Core.NinoTuple<int,long>>(byte&)
		// Nino.Core.NinoTuple<long,Unity.Mathematics.float3> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>>(byte&)
		// Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>>(byte&)
		// Nino.Core.NinoTuple<long,long> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Nino.Core.NinoTuple<long,long>>(byte&)
		// System.Collections.Generic.KeyValuePair<int,long> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<System.Collections.Generic.KeyValuePair<int,long>>(byte&)
		// System.ValueTuple<long,long,int> System.Runtime.CompilerServices.Unsafe.ReadUnaligned<System.ValueTuple<long,long,int>>(byte&)
		// int System.Runtime.CompilerServices.Unsafe.ReadUnaligned<int>(byte&)
		// long System.Runtime.CompilerServices.Unsafe.ReadUnaligned<long>(byte&)
		// uint System.Runtime.CompilerServices.Unsafe.ReadUnaligned<uint>(byte&)
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<ET.ActorId>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Nino.Core.NinoTuple<int,ET.ActorId>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Nino.Core.NinoTuple<int,int,long,ET.ActorId>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Nino.Core.NinoTuple<int,int,long>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Nino.Core.NinoTuple<int,int>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Nino.Core.NinoTuple<int,long,ET.ActorId>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Nino.Core.NinoTuple<int,long,long>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Nino.Core.NinoTuple<int,long>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Nino.Core.NinoTuple<long,long>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<System.Collections.Generic.KeyValuePair<int,long>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<System.ValueTuple<long,long,int>>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Unity.Mathematics.float3>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Unity.Mathematics.float3>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<byte>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<byte>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<int>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<int>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<long>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<long>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<uint>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<uint>()
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<ET.ActorId>(byte&,ET.ActorId)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>>(byte&,Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Nino.Core.NinoTuple<int,ET.ActorId>>(byte&,Nino.Core.NinoTuple<int,ET.ActorId>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>>(byte&,Nino.Core.NinoTuple<int,Unity.Mathematics.float3>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>>(byte&,Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>>(byte&,Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Nino.Core.NinoTuple<int,int,long,ET.ActorId>>(byte&,Nino.Core.NinoTuple<int,int,long,ET.ActorId>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Nino.Core.NinoTuple<int,int,long>>(byte&,Nino.Core.NinoTuple<int,int,long>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Nino.Core.NinoTuple<int,int>>(byte&,Nino.Core.NinoTuple<int,int>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Nino.Core.NinoTuple<int,long,ET.ActorId>>(byte&,Nino.Core.NinoTuple<int,long,ET.ActorId>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>>(byte&,Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Nino.Core.NinoTuple<int,long,long>>(byte&,Nino.Core.NinoTuple<int,long,long>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Nino.Core.NinoTuple<int,long>>(byte&,Nino.Core.NinoTuple<int,long>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>>(byte&,Nino.Core.NinoTuple<long,Unity.Mathematics.float3>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>>(byte&,Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Nino.Core.NinoTuple<long,long>>(byte&,Nino.Core.NinoTuple<long,long>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<System.Collections.Generic.KeyValuePair<int,long>>(byte&,System.Collections.Generic.KeyValuePair<int,long>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<System.ValueTuple<long,long,int>>(byte&,System.ValueTuple<long,long,int>)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<int>(byte&,int)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<long>(byte&,long)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<uint>(byte&,uint)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<ushort>(byte&,ushort)
		// System.ReadOnlySpan<byte> System.Runtime.InteropServices.MemoryMarshal.AsBytes<uint>(System.ReadOnlySpan<uint>)
		// System.Span<byte> System.Runtime.InteropServices.MemoryMarshal.AsBytes<Unity.Mathematics.float3>(System.Span<Unity.Mathematics.float3>)
		// System.Span<byte> System.Runtime.InteropServices.MemoryMarshal.AsBytes<byte>(System.Span<byte>)
		// System.Span<byte> System.Runtime.InteropServices.MemoryMarshal.AsBytes<int>(System.Span<int>)
		// System.Span<byte> System.Runtime.InteropServices.MemoryMarshal.AsBytes<long>(System.Span<long>)
		// System.ReadOnlySpan<uint> System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpan<uint>(uint&,int)
		// System.Span<ET.ActorId> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<ET.ActorId>(ET.ActorId&,int)
		// System.Span<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>>(Nino.Core.NinoTuple<Unity.Mathematics.quaternion,int>&,int)
		// System.Span<Nino.Core.NinoTuple<int,ET.ActorId>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<Nino.Core.NinoTuple<int,ET.ActorId>>(Nino.Core.NinoTuple<int,ET.ActorId>&,int)
		// System.Span<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<Nino.Core.NinoTuple<int,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<int,Unity.Mathematics.float3>&,int)
		// System.Span<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,ET.ActorId>&,int)
		// System.Span<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId,int>&,int)
		// System.Span<Nino.Core.NinoTuple<int,int,long,ET.ActorId>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<Nino.Core.NinoTuple<int,int,long,ET.ActorId>>(Nino.Core.NinoTuple<int,int,long,ET.ActorId>&,int)
		// System.Span<Nino.Core.NinoTuple<int,int,long>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<Nino.Core.NinoTuple<int,int,long>>(Nino.Core.NinoTuple<int,int,long>&,int)
		// System.Span<Nino.Core.NinoTuple<int,int>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<Nino.Core.NinoTuple<int,int>>(Nino.Core.NinoTuple<int,int>&,int)
		// System.Span<Nino.Core.NinoTuple<int,long,ET.ActorId>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<Nino.Core.NinoTuple<int,long,ET.ActorId>>(Nino.Core.NinoTuple<int,long,ET.ActorId>&,int)
		// System.Span<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>>(Nino.Core.NinoTuple<int,long,Unity.Mathematics.float3,Unity.Mathematics.quaternion>&,int)
		// System.Span<Nino.Core.NinoTuple<int,long,long>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<Nino.Core.NinoTuple<int,long,long>>(Nino.Core.NinoTuple<int,long,long>&,int)
		// System.Span<Nino.Core.NinoTuple<int,long>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<Nino.Core.NinoTuple<int,long>>(Nino.Core.NinoTuple<int,long>&,int)
		// System.Span<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<Nino.Core.NinoTuple<long,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<long,Unity.Mathematics.float3>&,int)
		// System.Span<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>>(Nino.Core.NinoTuple<long,int,int,Unity.Mathematics.float3,Unity.Mathematics.float3>&,int)
		// System.Span<Nino.Core.NinoTuple<long,long>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<Nino.Core.NinoTuple<long,long>>(Nino.Core.NinoTuple<long,long>&,int)
		// System.Span<System.Collections.Generic.KeyValuePair<int,long>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<System.Collections.Generic.KeyValuePair<int,long>>(System.Collections.Generic.KeyValuePair<int,long>&,int)
		// System.Span<System.ValueTuple<long,long,int>> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<System.ValueTuple<long,long,int>>(System.ValueTuple<long,long,int>&,int)
		// System.Span<int> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<int>(int&,int)
		// System.Span<long> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<long>(long&,int)
		// System.Span<uint> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<uint>(uint&,int)
		// Unity.Mathematics.float3& System.Runtime.InteropServices.MemoryMarshal.GetReference<Unity.Mathematics.float3>(System.Span<Unity.Mathematics.float3>)
		// byte& System.Runtime.InteropServices.MemoryMarshal.GetReference<byte>(System.ReadOnlySpan<byte>)
		// byte& System.Runtime.InteropServices.MemoryMarshal.GetReference<byte>(System.Span<byte>)
		// int& System.Runtime.InteropServices.MemoryMarshal.GetReference<int>(System.Span<int>)
		// long& System.Runtime.InteropServices.MemoryMarshal.GetReference<long>(System.Span<long>)
		// uint& System.Runtime.InteropServices.MemoryMarshal.GetReference<uint>(System.ReadOnlySpan<uint>)
		// object UnityEngine.GameObject.GetComponent<object>()
		// YooAsset.AllAssetsHandle YooAsset.ResourcePackage.LoadAllAssetsAsync<object>(string,uint)
		// YooAsset.AssetHandle YooAsset.ResourcePackage.LoadAssetAsync<object>(string,uint)
		// string string.Join<int>(string,System.Collections.Generic.IEnumerable<int>)
		// string string.JoinCore<int>(System.Char*,int,System.Collections.Generic.IEnumerable<int>)
	}
}