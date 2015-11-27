Boring Testing Kata
===================

This is just a test showing some of the things we can do. There is not
much to see here. 

The translation to JavaScript is now done in a better way and so your
source code can contain types too (woohoo!)

********************************************************************************

# Playground 1

Look, we can test things
------------------------

```source
type Foo = Left | Right

let flip whatever = Left
```

foo

### Flip left is right

```test
flip Left = Right
```

### Flip right is left

```test
flip 
  Right = 
    Left
```

--------------------------------------------------------------------------------

# Playground 2

Look, we can test more things
------------------------

```source
let add a b = a + b
```

foo

### Adding 1 and 2

```test
add 1 2 = 3
```

```test
add 1 3 = 4
```


