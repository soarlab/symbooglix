const a:bv8;
axiom a != 7bv8;

// Bitvector functions
function {:bvbuiltin "bvsub"} bv8sub(bv8,bv8) returns(bv8);

procedure main()
{
    var x:bv8;
    assert {:symbooglix_bp "entry"} true;
    assert a == 7bv8;
}
