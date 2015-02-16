from Project import *

def test_stack(aStack):
    aStack.push(0)
    aStack.push(8)
    aStack.push(55)
    assert(aStack.pop() == 55)
    assert(aStack.peek() == 8)
    assert(aStack.pop() == 8)
    aStack.push(99)
    assert(aStack.pop() == 99)
    assert(aStack.pop() == 0)
    
def test_list(aList):
    aList.insert(0, 55)
    aList.add(77)
    aList.insert(1, 33)
    for i in range(0, 200):
        aList.add(5)
    assert(aList[0] == 55)
    assert(aList[1] == 33)
    assert(aList[2] == 77)
    assert(aList[aList.count - 1] == 5)
    aList.remove_at(8)
    assert(aList[0] == 55)
    assert(aList[1] == 33)
    assert(aList[2] == 77)
    assert(aList[aList.count - 1] == 5)
    aList.remove_at(0)
    assert(aList[0] == 33)
    assert(aList[1] == 77)
    assert(aList[aList.count - 1] == 5)
    aList.remove_at(aList.count - 1)
    assert(aList[0] == 33)
    assert(aList[1] == 77)
    assert(aList[aList.count - 1] == 5)
    aList.insert(aList.count, 2)
    assert(aList[aList.count - 1] == 2)
    
def assert_kv_contents(container, oracle):
    assert(len(container) == len(oracle))
    for i in range(len(container)):
        assert(container[i].key == oracle[i][0])

def test_table(aTable):
    """ Tests the workings of a table by comparing it to the default Python dictionary. """
    pyDict = {}
    aTable[55] = 55
    pyDict[55] = 55
    aTable[19] = 5
    pyDict[19] = 5
    aTable[22] = 99
    pyDict[22] = 99
    aTable[9] = 40
    pyDict[9] = 40
    assert_kv_contents(sorted(aTable.to_list().to_array()), sorted(pyDict.items()))
    assert(aTable.remove(55))
    del pyDict[55]
    assert_kv_contents(sorted(aTable.to_list().to_array()), sorted(pyDict.items()))

print("Testing linked list")
test_list(LinkedList())

print("Testing array list")
test_list(ArrayList())

print("Testing stack with linked list.")
test_stack(Stack())

print("Testing stack with array list.")
test_stack(Stack(ArrayList()))

print("Testing binary tree")
test_table(BinaryTreeTable())

print("All tests successful")