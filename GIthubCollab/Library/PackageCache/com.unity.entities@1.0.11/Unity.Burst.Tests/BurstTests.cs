using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using NUnit.Framework;
using static Unity.Burst.CompilerServices.Aliasing;

public class BurstTestFixture
{
    AllocatorHelper<RewindableAllocator> m_AllocatorHelper;
    protected ref RewindableAllocator RwdAllocator => ref m_AllocatorHelper.Allocator;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        m_AllocatorHelper = new AllocatorHelper<RewindableAllocator>(Allocator.Persistent);
        m_AllocatorHelper.Allocator.Initialize(128 * 1024, true);
    }

    [OneTimeTearDown]
    public virtual void OneTimeTearDown()
    {
        m_AllocatorHelper.Allocator.Dispose();
        m_AllocatorHelper.Dispose();
    }

    [TearDown]
    public virtual void TearDown()
    {
        RwdAllocator.Rewind();
        // This is test only behavior for determinism.  Rewind twice such that all
        // tests start with an allocator containing only one memory block.
        RwdAllocator.Rewind();
    }
}


[BurstCompile]
public class BurstTests : BurstTestFixture
{
    [BurstCompile(CompileSynchronously = true)]
    public struct SimpleArrayAssignJob : IJob
    {
        public float value;
        public NativeArray<float> input;
        public NativeArray<float> output;

        public void Execute()
        {
            for (int i = 0; i != output.Length; i++)
                output[i] = i + value + input[i];
        }
    }

    [Test]
    public void SimpleFloatArrayAssignSimple()
    {
        var job = new SimpleArrayAssignJob();
        job.value = 10.0F;
        job.input = new NativeArray<float>(3, Allocator.Persistent);
        job.output = new NativeArray<float>(3, Allocator.Persistent);

        for (int i = 0; i != job.input.Length; i++)
            job.input[i] = 1000.0F * i;

        job.Schedule().Complete();

        Assert.AreEqual(3, job.output.Length);
        for (int i = 0; i != job.output.Length; i++)
            Assert.AreEqual(i + job.value + job.input[i], job.output[i]);

        job.input.Dispose();
        job.output.Dispose();
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct SimpleArrayAssignForJob : IJobParallelFor
    {
        public float value;
        public NativeArray<float> input;
        public NativeArray<float> output;

        public void Execute(int i)
        {
            output[i] = i + value + input[i];
        }
    }

    [Test]
    public void SimpleFloatArrayAssignForEach()
    {
        var job = new SimpleArrayAssignForJob();
        job.value = 10.0F;
        job.input = new NativeArray<float>(1000, Allocator.Persistent);
        job.output = new NativeArray<float>(1000, Allocator.Persistent);

        for (int i = 0; i != job.input.Length; i++)
            job.input[i] = 1000.0F * i;

        job.Schedule(job.input.Length, 40).Complete();

        Assert.AreEqual(1000, job.output.Length);
        for (int i = 0; i != job.output.Length; i++)
            Assert.AreEqual(i + job.value + job.input[i], job.output[i]);

        job.input.Dispose();
        job.output.Dispose();
    }

    [BurstCompile(CompileSynchronously = true)]
    struct MallocTestJob : IJob
    {
        unsafe public void Execute()
        {
            void* allocated = Memory.Unmanaged.Allocate(UnsafeUtility.SizeOf<int>() * 100, 4, Allocator.Persistent);
            Memory.Unmanaged.Free(allocated, Allocator.Persistent);
        }
    }

    [Test]
    public void MallocTest()
    {
        var jobData = new MallocTestJob();
        jobData.Run();
    }

    [BurstCompile(CompileSynchronously = true)]
    struct ListCapacityJob : IJob
    {
        public NativeList<int> list;
        public void Execute()
        {
            list.Capacity = 100;
        }
    }

    [Test]
    public void ListCapacityJobTest()
    {
        var jobData = new ListCapacityJob() { list = new NativeList<int>(RwdAllocator.ToAllocator) };
        jobData.Run();

        Assert.IsTrue(jobData.list.Capacity >= 100);
        Assert.AreEqual(0, jobData.list.Length);

        jobData.list.Dispose();
    }

    [BurstCompile(CompileSynchronously = true)]
    struct NativeListAssignValue : IJob
    {
        public NativeList<int> list;

        public void Execute()
        {
            list[0] = list[1];
        }
    }

    [Test]
    public void AssignValue()
    {
        var allocatorHelper = new AllocatorHelper<RewindableAllocator>(Allocator.Persistent);
        ref var allocator = ref allocatorHelper.Allocator;
        allocator.Initialize(128 * 1024, true);

        var jobData = new NativeListAssignValue() { list = new NativeList<int>(allocator.ToAllocator) };
        jobData.list.Add(5);
        jobData.list.Add(42);

        jobData.Run();

        Assert.AreEqual(2, jobData.list.Length);
        Assert.AreEqual(jobData.list[0], jobData.list[1]);

        jobData.list.Dispose();
        allocator.Dispose();
        allocatorHelper.Dispose();
    }

    [BurstCompile(CompileSynchronously = true)]
    struct NativeListAddValue : IJob
    {
        public NativeList<int> list;

        public void Execute()
        {
            list.Add(1);
            list.Add(2);
        }
    }

    [Test]
    public void AddValue()
    {
        var jobData = new NativeListAddValue() { list = new NativeList<int>(1, Allocator.Persistent) };

        jobData.list.Add(-1);

        jobData.Run();

        Assert.AreEqual(3, jobData.list.Length);
        Assert.AreEqual(-1, jobData.list[0]);
        Assert.AreEqual(1, jobData.list[1]);
        Assert.AreEqual(2, jobData.list[2]);

        jobData.list.Dispose();
    }

    private struct SomeStruct
    {
        public int a;
        public int b;
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct NativeListAliasingJob : IJob
    {
        public NativeArray<float> a0;

        public NativeList<int> l0;

        public NativeList<SomeStruct> l1;

        [NativeDisableContainerSafetyRestriction]
        public NativeList<int> l2;

        public unsafe void Execute()
        {
            // The three [NativeContainer]'s do not alias with each other.
            ExpectNotAliased(a0.GetUnsafePtr(), l0.GetUnsafePtr());
            ExpectNotAliased(a0.GetUnsafePtr(), l1.GetUnsafePtr());
            ExpectNotAliased(l0.GetUnsafePtr(), l1.GetUnsafePtr());

            // But they can all alias with l2 since it has the container safety disabled.
            ExpectAliased(a0.GetUnsafePtr(), l2.GetUnsafePtr());
            ExpectAliased(l0.GetUnsafePtr(), l2.GetUnsafePtr());
            ExpectAliased(l1.GetUnsafePtr(), l2.GetUnsafePtr());
        }
    }

    [Test]
    public void NativeListAliasing()
    {
        var jobData = new NativeListAliasingJob()
        {
            a0 = new NativeArray<float>(1, Allocator.Persistent),
            l0 = new NativeList<int>(1, Allocator.Persistent),
            l1 = new NativeList<SomeStruct>(1, Allocator.Persistent),
            l2 = new NativeList<int>(1, Allocator.Persistent),
        };

        jobData.l1.Add(new SomeStruct { a = 42, b = 13 });

        jobData.Run();

        jobData.a0.Dispose();
        jobData.l0.Dispose();
        jobData.l1.Dispose();
        jobData.l2.Dispose();
    }

    unsafe struct ConditionalTestStruct
    {
        public void* a;
        public void* b;
    }

    [BurstCompile(CompileSynchronously = true)]
    unsafe struct PointerConditional : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public ConditionalTestStruct* t;

        public void Execute()
        {
            t->b = t->a != null ? t->a : null;
        }
    }

    [Test]
    public unsafe void PointerConditionalDoesntCrash()
    {
        ConditionalTestStruct dummy;
        dummy.a = (void*)0x1122334455667788;
        dummy.b = null;
        var jobData = new PointerConditional { t = &dummy };
        jobData.Schedule().Complete();
        Assert.AreEqual((IntPtr)dummy.a, (IntPtr)dummy.b);
    }

    [BurstCompile(CompileSynchronously = true)]
    unsafe struct TempAllocationJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public int* res;

        public void Execute()
        {
            var array = new NativeArray<int>(10, Allocator.Temp);
            for (int i = 0; i != array.Length; i++)
                array[i] = 10;
            for (int i = 0; i != array.Length; i++)
                *res += array[i];
        }
    }

    [Test]
    public unsafe void TempAllocationInJob()
    {
        int value = 0;
        TempAllocationJob job;
        job.res = &value;
        job.Schedule().Complete();
        Assert.AreEqual(100, value);
    }

    public struct Data : IBufferElementData
    {
        public int Payload;

        public static implicit operator Data(int i) => new Data { Payload = i };
        public static implicit operator int(Data d) => d.Payload;
    }

    [BurstCompile(CompileSynchronously = true)]
    public static int MinInDynamicBuffer(in DynamicBuffer<Data> buffer)
    {
        var min = int.MaxValue;

        foreach (var element in buffer)
        {
            min = Math.Min(min, element);
        }

        return min;
    }

    public delegate int MinInDynamicBufferDelegate(in DynamicBuffer<Data> buffer);

    [Test]
    public void DynamicBuffer()
    {
        using var world = new World("Test World");

        var entity = world.EntityManager.CreateEntity();

        var buffer = world.EntityManager.AddBuffer<Data>(entity);

        buffer.Add(42);
        buffer.Add(13);
        buffer.Add(-4);

        var d = BurstCompiler.CompileFunctionPointer<MinInDynamicBufferDelegate>(MinInDynamicBuffer);

        Assert.AreEqual(-4, d.Invoke(in buffer));
    }

    [Test]
    public void DirectCallDynamicBuffer()
    {
        using var world = new World("Test World");
        var entity = world.EntityManager.CreateEntity();

        var buffer = world.EntityManager.AddBuffer<Data>(entity);

        buffer.Add(42);
        buffer.Add(13);
        buffer.Add(-4);

        Assert.AreEqual(-4, MinInDynamicBuffer(in buffer));
    }

#if UNITY_EDITOR
    [BurstCompile(CompileSynchronously = true)]
    unsafe public static void DoThing(void* val)
    {
        throw new ArgumentException();
    }
    unsafe public delegate void FunPtr(void* val);

    [Test]
    unsafe public void FunctionPointerExceptions()
    {
        var func = BurstCompiler.CompileFunctionPointer<FunPtr>(DoThing).Invoke;

        bool didCatch = false;
        try
        {
            func(null);
        }
        catch (Exception)
        {
            didCatch = true;
        }

        Assert.IsTrue(didCatch);
    }

    [Test]
    [Ignore("This currently fails in Unity 2020.3. Burst team claims that this is fixed in 2022.1. Once it is fixed we can further reduce the amount of BurstCompiler.CompileFunctionPointer code we use")]
    unsafe public void DirectCallExceptions()
    {
        bool didCatch = false;
        try
        {
            DoThing(null);
        }
        catch (Exception)
        {
            didCatch = true;
        }

        Assert.IsTrue(didCatch);
    }
#endif
}
