# ===========================================
# TwoThreeTreeTest.py
# Tests for Othman Nahhas' 2-3 tree implementation based on his contract for said data type
# Jonathan Van der Cruysse - Informatica Ba 1
# ===========================================
# Module 'ProjectDataTypes' should contain 'TreeTable', 'DefaultRecordMap' and all supporting interfaces (such as IRecord).
# Module 'TwoThreeTreeModule' should contain 'TwoThreeTree', and may rely on 'ProjectDataTypes'.
# All references to 'TwoThreeTree' can be replaced by 'BinarySearchTree', or by any other tree implementation. The tests should continue to work.
# ===========================================
# These tests are randomized to reduce the odds of missing errors due to a highly specific set of test data.
# They do not test the 'TwoThreeTree' class directly, but use a 'TreeTable' with the tree as backing storage instead.
# The advantage of this approach is that it allows for the table to be compared against the reference Python dictionary, which is assumed to be correct.
# Also, it is rather unlikely that a 'TwoThreeTree' would contain bugs that would not be caught by using it as backing storage for a 'TreeTable', as said type uses just about all methods of a tree.
# It also makes sense to use the tree as backing storage for a table in testing, as this will likely be the only situation it will be used in.

# Imports all previously created dependencies, like 'TreeTable'
from ProjectDataTypes import *
# Imports the 2-3 tree implementation from the module. Change the module and type names of necessary.
from TwoThreeTreeModule import TwoThreeTree
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

print("Testing 2-3 tree table...")
# Replace 'TwoThreeTree' with another tree type to test the other tree type
test_table(TreeTable(TwoThreeTree(DefaultRecordMap())))

print("All tests successful")