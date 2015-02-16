# ===========================================
# TableTest.py
# Jonathan Van der Cruysse - Informatica Ba 1
# ===========================================
# I did not recieve any tests for my contract, so I wrote my own.
# The following tests are to test my contract and implementation of a hash table.
# ===========================================
# These tests are randomized to reduce the odds of missing errors due to a highly specific set of test data.

from ProjectDataTypes import *
import functools
import random
    
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
        if aTable.contains_key(value):
            assert(value in pyDict)
            assert(symmetric_table_remove(aTable, pyDict, value))
        else:
            assert(not symmetric_table_remove(aTable, pyDict, value))
    
    assert_kv_contents(sorted(to_py_list(aTable.to_list())), sorted(pyDict.items()))

def test_swap_table():
    """ Tests class 'SwapTable' by interleaving the normal table test with random swaps. """

    aTable = SwapTable(Hashtable(DefaultRecordMap(), BinaryTreeTableFactory()))

    pyDict = {}

    rng = random.Random()

    for i in range(0, 100):
        diff = aTable.count - len(pyDict);
        assert(diff == 0)
        record = IntRecord(rng.randrange(-100, 100))

        swap_rand = rng.randrange(0, 20)

        if swap_rand == 0: # One in twenty chance to swap to a separate chaining hashtable
            aTable.swap(Hashtable(DefaultRecordMap(), BinaryTreeTableFactory()))
        elif swap_rand == 1: # One in twenty chance to swap to an open addressed hashtable
            aTable.swap(OpenHashtable(DefaultRecordMap(), PowerSequenceMap(1)))
        elif swap_rand == 2: # One in twenty chance to swap to a binary search tree
            aTable.swap(TreeTable(BinarySearchTree(DefaultRecordMap())))

        if not aTable.contains_key(record.key):
            assert(record.key not in pyDict)
            assert(symmetric_table_insert(aTable, pyDict, record))
        else:
            assert(not symmetric_table_insert(aTable, pyDict, record))

    for i in range(0, 200):
        value = rng.randrange(-100, 100)
        diff = aTable.count - len(pyDict);
        assert(diff == 0)
        if aTable.contains_key(value):
            assert(value in pyDict)
            assert(symmetric_table_remove(aTable, pyDict, value))
        else:
            assert(not symmetric_table_remove(aTable, pyDict, value))
    
    assert_kv_contents(sorted(to_py_list(aTable.to_list())), sorted(pyDict.items()))

print("Testing separate chaining hash table...")
test_table(Hashtable(DefaultRecordMap(), BinaryTreeTableFactory()))

print("Testing linear open addressing hash table...")
test_table(OpenHashtable(DefaultRecordMap(), PowerSequenceMap(1)))

print("Testing quadratic open addressing hash table...")
test_table(OpenHashtable(DefaultRecordMap(), PowerSequenceMap(2)))

print("Testing swap table...")
test_swap_table()

print("All tests successful")