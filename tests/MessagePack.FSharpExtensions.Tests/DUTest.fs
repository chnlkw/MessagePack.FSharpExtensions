module MessagePack.Tests.DUTest

open Xunit
open MessagePack

let convert_nongeneric (value: 'a) =
  let resolver = WithFSharpDefaultResolver() :> IFormatterResolver
  let t = value.GetType()
  let bin : byte[] = MessagePackSerializer.NonGeneric.Serialize(t, value :> obj, resolver)
  let actual :'a = MessagePackSerializer.NonGeneric.Deserialize(t, bin, resolver) :?> 'a
  Assert.Equal<'a>(value, actual)

[<MessagePackObject>]
type SimpleUnion =
  | A
  | B of int
  | C of int64 * float32

[<MessagePackObject>]
type SimpleUnion2 =
  | A2
  | AU2 of unit
  | B2 of int
  | C2 of int64 * float32

[<Fact>] // failed
let sampleA () =
    convert_nongeneric A

[<Fact>]
let ``sampleA2`` () =
    convert_nongeneric A2

[<Fact>]
let sampleAU () =
    convert_nongeneric <| AU2 ()
    
[<Fact>]
let sampleB () =
    convert_nongeneric (B 1)
    convert_nongeneric (B2 1)

[<Fact>]
let sampleC () =
    convert_nongeneric <| C(1L, 1.0f)
    convert_nongeneric <| C2(1L, 1.0f)


[<Fact>]
let simple () =

  let input = A
  let actual = convert input
  Assert.Equal(input, actual)

  let input = B 100
  let actual = convert input
  Assert.Equal(input, actual)

  let input = C(99999999L, -123.43f)
  let actual = convert input
  Assert.Equal(input, actual)

type StringKeyUnion = | D of Prop : int

[<Fact>]
let ``string key`` () =

  let input = D 1
  let actual = convert input
  Assert.Equal(input, actual)

let mutable beforeCallback = false
let mutable afterCallback = false

[<MessagePackObject>]
type CallbackUnion =
  | Call
with
  interface IMessagePackSerializationCallbackReceiver with
    override this.OnBeforeSerialize() =
      beforeCallback <- true
    override this.OnAfterDeserialize() =
      afterCallback <- true

[<Fact>]
let ``receive callback`` () =
  let input = Call
  let actual = convert input
  Assert.True(beforeCallback)
  Assert.True(afterCallback)

module Compatibility =

  open MessagePack.Resolvers
  open MessagePack.FSharp

  let convert<'T, 'U> (value: 'T) =
    let resolver = WithFSharpDefaultResolver() :> IFormatterResolver
    MessagePackSerializer.Deserialize<'U>(MessagePackSerializer.Serialize(value, resolver), resolver)

  [<Union(0, typeof<CsA>)>]
  [<Union(1, typeof<CsB>)>]
  [<Union(2, typeof<CsC>)>]
  type CsSimpleUnion = interface end

  and [<MessagePackObject>]CsA() =
    interface CsSimpleUnion

  and [<MessagePackObject>]CsB() =

    [<Key(0)>]
    member val Item: int = 0 with get, set

    interface CsSimpleUnion

  and [<MessagePackObject>]CsC() =

    [<Key(0)>]
    member val Item1: int64 = 0L with get, set

    [<Key(1)>]
    member val Item2: float32 = 0.0f with get, set

    interface CsSimpleUnion

  [<Fact>]
  let simple () =

    let input = A
    let actual = convert<SimpleUnion, CsSimpleUnion> input |> box
    Assert.True(actual :? CsA)

    let input = B 100
    match convert<SimpleUnion, CsSimpleUnion> input |> box with
    | :? CsB as actual ->
      Assert.Equal(100, actual.Item)
    | actual -> Assert.True(false, sprintf "expected: CsB, but was: %A" actual)

    let input = C(99999999L, -123.43f)
    match convert<SimpleUnion, CsSimpleUnion> input  |> box with
    | :? CsC as actual ->
      Assert.Equal(99999999L, actual.Item1)
      Assert.Equal(-123.43f, actual.Item2)
    | actual -> Assert.True(false, sprintf "expected: CsC, but was: %A" actual)

  [<Union(0, typeof<CsD>)>]
  type CsStringKeyUnion = interface end

  and [<MessagePackObject>]CsD() =

    [<Key("Prop")>]
    member val Prop: int = 0 with get, set

    interface CsStringKeyUnion

  [<Fact>]
  let ``string key`` () =

    let input = D 100
    match convert<StringKeyUnion, CsStringKeyUnion> input  |> box with
    | :? CsD as actual ->
      Assert.Equal(100, actual.Prop)
    | actual -> Assert.True(false, sprintf "expected: CsD, but was: %A" actual)
