module Array

let swap (array: array<'a>) n k =
    let copy = array[n]
    array[n] <- array[k]
    array[k] <- copy
