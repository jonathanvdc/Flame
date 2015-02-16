# ===========================================
# ProjectTest.py
# Jonathan Van der Cruysse - Informatica Ba 1
# ===========================================
# These tests are randomized to reduce the odds of missing errors due to a highly specific set of test data.

from Project import *
import functools
import random

def test_stack(aStack):
    """ Tests stack functionality by comparing it to a python stack. """
    
    pyStack = []

    rng = random.Random()

    # Set push probability to 2 / 3, to make sure that the stack reaches an acceptable size.
    for i in range(100):
        push = rng.randrange(0, 3) < 2

        if push:
            value = rng.randrange(-100, 100)
            aStack.push(value)
            assert(aStack.top == value)
            pyStack.append(value)
            assert(aStack.count == len(pyStack))
        else:
            if aStack.count <= 0:
                assert(aStack.pop() is None)
                assert(aStack.top is None)
            else:
                assert(aStack.pop() == pyStack.pop())
                assert(aStack.count == len(pyStack))

    while aStack.count > 0:
        assert(aStack.pop() == pyStack.pop())
        assert(aStack.count == len(pyStack))

    assert(aStack.pop() is None)
    assert(aStack.top is None)
    
def test_list(aList):
    """ Tests list functionality by comparing it to a python list. """

    pyList = []

    rng = random.Random()

    # Set first 20 elements. Otherwise, tons of random indices are 'wasted' in the second loop because the first item would only be inserted when rng.randrange(-10, 100) returns zero.
    for i in range(0, 20):
        val = rng.randrange(-100, 100)
        aList.add(val)
        pyList.append(val)

    # Now do some random inserts and removes. Probability of insert = 2 / 3, so the list reaches a respectable size.
    for i in range(0, 500):
        pos = rng.randint(-10, 100)
        val = rng.randint(-100, 100)
        insert = rng.randrange(0, 3) < 2
        if insert:
            if pos < 0 or pos > aList.count:
                assert(not aList.insert(pos, val))
            else:
                assert(aList.insert(pos, val))
                pyList.insert(pos, val)
        else:
            if pos < 0 or pos >= aList.count:
                assert(not aList.remove_at(pos))
            else:
                assert(aList.remove_at(pos))
                del pyList[pos]
                
    for a, b in zip(aList, pyList):
        assert(a == b)

    assert(to_py_list(aList) == pyList)
    
def assert_kv_contents(container, oracle):
    """ Checks if the keys of the records in 'container' match the keys of the key-value pairs in 'oracle' """
    assert(len(container) == len(oracle))
    for i in range(len(container)):
        assert(container[i].key == oracle[i][0])

def symmetric_table_insert(aTable, pyDict, record):
    """ Adds a record to the given table and performs the same operation on a python dictionary. """
    val = aTable.insert(record)
    assert(aTable.contains_key(record.key))
    pyDict[record.key] = record
    return val
    
def symmetric_table_remove(aTable, pyDict, key):
    """ Removes a record associated with the provided key from the given table and performs the same operation on a python dictionary. """
    val = aTable.remove(key)
    assert(val == (key in pyDict))
    assert(not aTable.contains_key(key))
    if key in pyDict:
        del pyDict[key]
    return val
    
@functools.total_ordering
class IntRecord(IRecord):
    """ A simple record that contains an integer, which is also the record's search key. """
    
    def __init__(self, value):
        self.value = value
    
    @property
    def key(self):
        return self.value
        
    def __lt__(self, other):
        return self.value < other.value
        
    def __eq__(self, other):
        return self.value == other.value

    def __str__(self):
        return self.value

    def __repr__(self):
        return "IntRecord(" + self.value + ")"
        
def to_py_list(aList):
    """ Converts the given IReadOnlyList to a python list. """
    
    l = []
    count = aList.count
    for i in range(count):
        l.append(aList[i])
    return l
        
def test_table(aTable):
    """ Tests the workings of a table by comparing it to the default Python dictionary. """
    pyDict = {}

    rng = random.Random()

    for i in range(0, 100):
        diff = aTable.count - len(pyDict);
        assert(diff == 0)
        assert(aTable.is_empty == (len(pyDict) == 0))
        record = IntRecord(rng.randrange(-100, 100))
        if not aTable.contains_key(record.key):
            assert(record.key not in pyDict)
            assert(symmetric_table_insert(aTable, pyDict, record))
        else:
            assert(not symmetric_table_insert(aTable, pyDict, record))

    for i in range(0, 200):
        value = rng.randrange(-100, 100)
        diff = aTable.count - len(pyDict);
        assert(diff == 0)
        assert(aTable.is_empty == (len(pyDict) == 0))
        if aTable.contains_key(value):
            assert(value in pyDict)
            assert(symmetric_table_remove(aTable, pyDict, value))
        else:
            assert(not symmetric_table_remove(aTable, pyDict, value))
    
    assert_kv_contents(sorted(to_py_list(aTable.to_list())), sorted(pyDict.items()))

def test_sorted_list(aList):
    """ Tests the workings of a sorted list by comparing it to a python sorted list. """
    l = []

    rng = random.Random()

    for i in range(0, 100):
        record = IntRecord(rng.randrange(-100, 100))
        aList.add(record)
        l.append(record)

    for i in range(0, 200):
        record = IntRecord(rng.randrange(-100, 100))
        assert(aList.contains(record) == (record in l))
        if aList.contains(record):
            oldCount = aList.count
            assert(aList.remove(record))
            assert(aList.count == oldCount - 1)
            assert(aList.is_empty == (len(l) == 0))
            l.remove(record)

    l.sort()
    
    assert(to_py_list(aList.to_list()) == l)

print("Testing linked list...")
test_list(LinkedList())

print("Testing array list...")
test_list(ArrayList())

print("Testing stack with linked list...")
test_stack(Stack(LinkedList()))

print("Testing stack with array list...")
test_stack(Stack(ArrayList()))

print("Testing binary tree table...")
test_table(TreeTable(BinarySearchTree(DefaultRecordMap())))

print("Testing separate chaining hash table...")
test_table(Hashtable(DefaultRecordMap(), BinaryTreeTableFactory()))

print("Testing linear open addressing hash table...")
test_table(OpenHashtable(DefaultRecordMap(), PowerSequenceMap(1)))

print("Testing quadratic open addressing hash table...")
test_table(OpenHashtable(DefaultRecordMap(), PowerSequenceMap(2)))

print("Testing binary tree sorted list...")
test_sorted_list(TreeSortedList(BinarySearchTree(DefaultRecordMap())))

print("All tests successful")