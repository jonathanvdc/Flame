class IReadOnlyCollection:
    """ Describes a generic read-only collection of items. """

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        raise NotImplementedError("Method 'IReadOnlyCollection.__iter__' was not implemented.")

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        raise NotImplementedError("Getter of property 'IReadOnlyCollection.count' was not implemented.")


class ICollection(IReadOnlyCollection):
    """ Describes a generic collection that allows items to be added. """

    def add(self, Item):
        """ Adds an item to the collection. """
        raise NotImplementedError("Method 'ICollection.add' was not implemented.")


class IReadOnlyList(IReadOnlyCollection):
    """ Describes a generic read-only, zero-based list. """

    def __getitem__(self, Index):
        """ Gets the item at the specified position in the list. """
        raise NotImplementedError("Method 'IReadOnlyList.__getitem__' was not implemented.")


class IList(IReadOnlyList, ICollection):
    """ Describes a generic list. """

    def insert(self, Index, Item):
        """ Inserts an item in the list at the specified position. """
        raise NotImplementedError("Method 'IList.insert' was not implemented.")

    def remove_at(self, Index):
        """ Removes the element at the specified index from the list. """
        raise NotImplementedError("Method 'IList.remove_at' was not implemented.")

    def __getitem__(self, Index):
        """ Gets the item at the specified position in the list. """
        raise NotImplementedError("Method 'IList.__getitem__' was not implemented.")

    def __setitem__(self, Index, value):
        """ Sets the item at the specified position in the list. """
        raise NotImplementedError("Method 'IList.__setitem__' was not implemented.")


class IRecord:
    """ Describes a generic record. """

    @property
    def key(self):
        """ Gets the record's search key. """
        raise NotImplementedError("Getter of property 'IRecord.key' was not implemented.")


class IMap:
    """ Describes a map, an object that maps source values to their target representation. It is essentially a pure mathematical function. """

    def map(self, Item):
        """ Maps the item to its target representation. """
        raise NotImplementedError("Method 'IMap.map' was not implemented.")


class ITable(IReadOnlyCollection):
    """ Describes a slightly modified version of the table ADT. """

    def insert(self, Item):
        """ Inserts an item into the table. """
        raise NotImplementedError("Method 'ITable.insert' was not implemented.")

    def remove(self, Key):
        """ Removes a key from the table. """
        raise NotImplementedError("Method 'ITable.remove' was not implemented.")

    def contains_key(self, Key):
        """ Finds out if the table contains the specified key. """
        raise NotImplementedError("Method 'ITable.contains_key' was not implemented.")

    def to_list(self):
        """ Gets the table's items as a read-only list. """
        raise NotImplementedError("Method 'ITable.to_list' was not implemented.")

    def __getitem__(self, Key):
        """ Retrieves the item in the table with the specified key. """
        raise NotImplementedError("Method 'ITable.__getitem__' was not implemented.")


class ITree:
    """ Describes a generic search tree. This is a generalization of a binary search tree that is also applicable to 2-3 trees, 2-3-4 trees, black-red trees and AVL trees. """

    def insert(self, Item):
        """ Inserts an item in the search tree. """
        raise NotImplementedError("Method 'ITree.insert' was not implemented.")

    def retrieve(self, Key):
        """ Retrieves the item with the specified key. """
        raise NotImplementedError("Method 'ITree.retrieve' was not implemented.")

    def remove(self, Key):
        """ Removes the item with the specified key from the search tree. """
        raise NotImplementedError("Method 'ITree.remove' was not implemented.")

    def traverse_inorder(self):
        """ Performs inorder traversal on the binary search tree and writes its items to a new list. """
        raise NotImplementedError("Method 'ITree.traverse_inorder' was not implemented.")

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the tree. """
        raise NotImplementedError("Method 'ITree.__iter__' was not implemented.")

    @property
    def key_map(self):
        """ Gets the function that maps the search tree's records to their search keys. """
        raise NotImplementedError("Getter of property 'ITree.key_map' was not implemented.")

    @property
    def count(self):
        """ Counts the number of items in the search tree. """
        raise NotImplementedError("Getter of property 'ITree.count' was not implemented.")

    @property
    def is_empty(self):
        """ Gets a boolean value that indicates if the tree is empty. """
        raise NotImplementedError("Getter of property 'ITree.is_empty' was not implemented.")


class IFactory:
    """ A generic interface for the factory pattern. It creates instances of type 'T' by using arguments of type 'TArg'. """

    def create(self, Argument):
        """ Creates a new instance from the provided argument. """
        raise NotImplementedError("Method 'IFactory.create' was not implemented.")


class ArrayList(IList):
    """ An array-based implementation of a list. """

    def __init__(self, data = None):
        """ Creates a new instance of a list backed by the provided array. """
        if data is None:
            self.data = [None] * 5
            self.elem_count = 0
            return
        self.data = data
        self.elem_count = len(data)

    def add(self, Item):
        """ Adds an item to the end of the list. """
        if self.count >= len(self.data):
            newData = [None] * (len(self.data) + 5)
            self.copy_to(newData)
            self.data = newData
        self.data[self.elem_count] = Item
        self.elem_count += 1

    def copy_to(self, Target):
        """ Copies the array list's contents to the provided target array. """
        for i in range(min(len(self.data), len(Target))):
            Target[i] = self.data[i]

    def insert(self, Index, Item):
        """ Inserts an item in the list at the specified position. """
        if Index == self.count:
            self.add(Item)
            return True
        elif Index < self.count and Index >= 0:
            self.shift_right(Index)
            self.data[Index] = Item
            self.elem_count += 1
            return True
        else:
            return False

    def shift_right(self, StartIndex):
        """ Shifts the elements in the list to the right from the provided index onward. """
        if self.count >= len(self.data):
            newData = [None] * (len(self.data) + 5)
            i = 0
            while i < StartIndex:
                newData[i] = self.data[i]
                i += 1
            i = StartIndex
            while i < self.count:
                newData[i + 1] = self.data[i]
                i += 1
            self.data = newData
        else:
            i = self.count - 1
            while i >= StartIndex:
                self.data[i + 1] = self.data[i]
                i -= 1

    def remove_at(self, Index):
        """ Removes the element at the specified index from the list. """
        if Index >= 0 and Index < self.count:
            self.shift_left(Index)
            self.elem_count -= 1
            return True
        else:
            return False

    def shift_left(self, StartIndex):
        """ Shifts the elements in the list to the left from the provided index onward. """
        i = StartIndex + 1
        while i < self.count:
            self.data[i - 1] = self.data[i]
            i += 1

    def to_array(self):
        """ Gets an array with length the number of elements in this list, and the same contents as this list. """
        arr = [None] * self.count
        self.copy_to(arr)
        return arr

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        i = 0
        while i < self.count:
            yield self.data[i]
            i += 1

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        return self.elem_count

    def __getitem__(self, Index):
        """ Gets the item in the list at the specified position. """
        return self.data[Index]

    def __setitem__(self, Index, value):
        """ Sets the item in the list at the specified position. """
        self.data[Index] = value


class ListNode:
    """ Describes a node in a linked list. """

    def __init__(self, Value):
        """ Creates a new linked list node instance from the specified value. """
        self.successor_value = None
        self.value = Value

    def insert_after(self, Value):
        """ Inserts a node containing the provided value after this node. """
        nextVal = ListNode(Value)
        nextVal.successor = self.successor
        self.successor = nextVal

    @property
    def value(self):
        """ Gets the value contained in the list node. """
        return self.value_value

    @value.setter
    def value(self, value):
        """ Sets the value contained in the list node. """
        self.value_value = value

    @property
    def successor(self):
        """ Gets the list node's successor node. """
        return self.successor_value

    @successor.setter
    def successor(self, value):
        """ Sets the list node's successor node. """
        self.successor_value = value

    @property
    def tail(self):
        """ Gets the "tail" node of this linked chain. """
        if self.successor is None:
            return self
        else:
            return self.successor.tail


class LinkedList(IList):
    """ Describes a linked list. """

    def __init__(self):
        """ Creates an empty linked list. """
        self.head_value = None

    def to_array(self):
        """ Gets an array representation of this linked list. """
        arr = [None] * self.count
        node = self.head
        i = 0
        while i < len(arr):
            arr[i] = node.value
            node = node.successor
            i += 1
        return arr

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        node = self.head
        while node is not None:
            yield node.value
            node = node.successor

    def node_at(self, Index):
        """ Gets the list node at the specified index. """
        if Index < 0:
            return None
        node = self.head
        i = 0
        while node is not None and i < Index:
            node = node.successor
            i += 1
        return node

    def item_at(self, Index):
        """ Gets the item in the linked list at the given index. """
        return self.node_at(Index).value

    def add(self, Item):
        """ Adds an item to the end of the linked list. """
        if self.head is None:
            self.head = ListNode(Item)
        else:
            self.tail.insert_after(Item)

    def remove_at(self, Index):
        """ Removes the item in the linked list at the specified index. """
        if Index >= 0 and Index < self.count:
            if Index == 0:
                self.head = self.head.successor
            else:
                predecessor = self.node_at(Index - 1)
                predecessor.successor = predecessor.successor.successor
            return True
        else:
            return False

    def insert(self, Index, Item):
        """ Inserts an item in the list at the specified position. """
        if Index == 0:
            oldHead = self.head
            self.head = ListNode(Item)
            if oldHead is not None:
                self.head.successor = oldHead
            return True
        node = self.node_at(Index - 1)
        if node is None:
            return False
        else:
            node.insert_after(Item)
            return True

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        node = self.head
        i = 0
        while node is not None:
            node = node.successor
            i += 1
        return i

    @property
    def head(self):
        """ Gets the linked list's head node. """
        return self.head_value

    @head.setter
    def head(self, value):
        """ Sets the linked list's head node. """
        self.head_value = value

    @property
    def tail(self):
        """ Gets the linked list's tail node. """
        if self.head is None:
            return None
        else:
            return self.head.tail

    def __getitem__(self, Index):
        """ Gets the item in the linked list at the given index. """
        return self.item_at(Index)

    def __setitem__(self, Index, value):
        """ Sets the item in the linked list at the given index. """
        self.node_at(Index).value = value


class BinaryTree:
    """ A link-based implementation of the Binary Tree ADT. """

    def __init__(self, Data):
        """ Creates a new binary tree from a data item. """
        self.left_value = None
        self.right_value = None
        self.data = Data

    def traverse_inorder(self, Target):
        """ Performs inorder traversal on the binary tree and writes its items to the given target collection. """
        if self.left is not None:
            self.left.traverse_inorder(Target)
        Target.add(self.data)
        if self.right is not None:
            self.right.traverse_inorder(Target)

    def copy_from(self, Other):
        """ Copies all information in the given binary tree into this binary tree. """
        if Other is None:
            self.data = None
            self.left = None
            self.right = None
        else:
            self.data = Other.data
            self.left = Other.left
            self.right = Other.right

    @property
    def left(self):
        """ Gets the binary tree's left subtree. """
        return self.left_value

    @left.setter
    def left(self, value):
        """ Sets the binary tree's left subtree. """
        self.left_value = value

    @property
    def data(self):
        """ Gets the binary tree's data, i.e. the record contained in the root. """
        return self.data_value

    @data.setter
    def data(self, value):
        """ Sets the binary tree's data, i.e. the record contained in the root. """
        self.data_value = value

    @property
    def right(self):
        """ Gets the binary tree's right subtree. """
        return self.right_value

    @right.setter
    def right(self, value):
        """ Sets the binary tree's right subtree. """
        self.right_value = value

    @property
    def count(self):
        """ Gets the number of items in the binary tree. """
        result = 1
        if self.left is not None:
            result += self.left.count
        if self.right is not None:
            result += self.right.count
        return result


class BinarySearchTree(ITree):
    """ Describes a binary search tree. """

    def __init__(self, KeyMap, tree = None):
        self.key_map = KeyMap
        self.tree = tree

    def traverse_inorder(self, Target = None):
        """ Performs inorder traversal on the binary search tree and writes its items to the given target collection. """
        if Target is None:
            aList = LinkedList()
            self.traverse_inorder(aList)
            return aList
        if self.tree is not None:
            self.tree.traverse_inorder(Target)

    def get_leftmost(self):
        """ Gets the binary tree's leftmost node. """
        if self.tree is None:
            return None
        else:
            ltree = self.left
            if ltree.is_empty:
                return self
            else:
                return ltree.get_leftmost()

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the tree. """
        rolist = self.traverse_inorder()
        i = 0
        while i < rolist.count:
            yield rolist[i]
            i += 1

    def insert(self, Item):
        """ Inserts an item in the binary search tree. """
        if self.tree is None:
            self.tree = BinaryTree(Item)
        else:
            recordKey = self.key_map.map(Item)
            if recordKey < self.key:
                if self.tree.left is None:
                    self.tree.left = BinaryTree(Item)
                else:
                    self.left.insert(Item)
            elif self.tree.right is None:
                self.tree.right = BinaryTree(Item)
            else:
                self.right.insert(Item)

    def retrieve(self, Key):
        """ Retrieves the item with the specified key. """
        if self.tree is None:
            return None
        elif Key == self.key:
            return self.tree.data
        elif Key < self.key:
            return self.left.retrieve(Key)
        else:
            return self.right.retrieve(Key)

    def remove(self, Key):
        """ Removes the item with the specified key from the binary search tree. """
        if self.is_empty:
            return False
        elif self.count == 1:
            if Key == self.key_map.map(self.tree.data):
                self.tree = None
                return True
            else:
                return False
        else:
            newTree = self.immutable_remove(Key)
            changed = not self.equals(newTree)
            self.tree.copy_from(newTree.tree)
            return changed

    def immutable_remove(self, Key):
        """ Returns a tree where one occurrence of an item with the provided key has been removed. Search trees are treated as immutable objects in this method. """
        if self.is_empty:
            return self
        ownKey = self.key
        if Key > ownKey:
            root = BinarySearchTree(self.key_map, BinaryTree(self.tree.data))
            root.tree.left = self.tree.left
            root.right = self.right.immutable_remove(Key)
            return root
        elif Key < ownKey:
            root = BinarySearchTree(self.key_map, BinaryTree(self.tree.data))
            root.left = self.left.immutable_remove(Key)
            root.tree.right = self.tree.right
            return root
        elif (not self.left.is_empty) and (not self.right.is_empty):
            root = BinarySearchTree(self.key_map, BinaryTree(self.right.get_leftmost().tree.data))
            root.tree.left = self.tree.left
            root.right = self.right.immutable_remove_leftmost()
            return root
        elif self.left.is_empty:
            return self.right
        else:
            return self.left

    def immutable_remove_leftmost(self):
        """ Returns a tree with the leftmost child removed. Search trees are treated as immutable objects in this method. """
        if self.is_empty:
            return self
        elif self.left.is_empty:
            return self.right
        else:
            root = BinarySearchTree(self.key_map, BinaryTree(self.tree.data))
            root.left = self.left.immutable_remove_leftmost()
            root.tree.right = self.tree.right
            return root

    def equals(self, Other):
        """ Compares two trees for equality.  This method is not intended for general use, and was specifically created for the 'Remove(TKey)' method. It uses reference comparison to achieve O(log(n)) performance. """
        if self.tree == Other.tree:
            return True
        elif self.is_empty or Other.is_empty:
            return self.is_empty == Other.is_empty
        else:
            if self.tree.data != Other.tree.data:
                return False
            return self.left.equals(Other.left) and self.right.equals(Other.right)

    @property
    def left(self):
        """ Gets the binary tree's left subtree. """
        if self.tree is None:
            return None
        else:
            return BinarySearchTree(self.key_map, self.tree.left)

    @left.setter
    def left(self, value):
        """ Sets the binary tree's left subtree. """
        self.tree.left = value.tree

    @property
    def key_map(self):
        """ Gets the function that maps the binary search tree's records to their search keys. """
        return self.key_map_value

    @key_map.setter
    def key_map(self, value):
        """ Sets the function that maps the binary search tree's records to their search keys. """
        self.key_map_value = value

    @property
    def is_empty(self):
        """ Gets a boolean value that indicates if the binary search tree is empty. """
        return self.tree is None

    @property
    def key(self):
        """ Gets the binary tree's root key. """
        if self.tree is None:
            return None
        else:
            return self.key_map.map(self.tree.data)

    @property
    def right(self):
        """ Gets the binary tree's right subtree. """
        if self.tree is None:
            return None
        else:
            return BinarySearchTree(self.key_map, self.tree.right)

    @right.setter
    def right(self, value):
        """ Sets the binary tree's right subtree. """
        self.tree.right = value.tree

    @property
    def count(self):
        """ Gets the number of items in the binary search tree. """
        if self.tree is None:
            return 0
        else:
            return self.tree.count

    @property
    def is_leaf(self):
        """ Gets a boolean value that indicates if the binary search tree is either empty, or a leaf. """
        return self.tree is None or self.tree.left is None and self.tree.right is None


class TreeTable(ITable):
    """ A search tree implementation of a table. """

    def __init__(self, tree):
        """ Creates a new tree implementation of a table, using the provided tree as backing storage. """
        self.tree = tree

    def insert(self, Item):
        """ Inserts an item into the table. """
        if not self.contains_key(self.key_map.map(Item)):
            self.tree.insert(Item)
            return True
        else:
            return False

    def contains_key(self, Key):
        """ Finds out if the table contains the specified key. """
        return self[Key] is not None

    def remove(self, Key):
        """ Removes a key from the table. """
        return self.tree.remove(Key)

    def to_list(self):
        """ Gets the table's items as a read-only list. """
        return self.tree.traverse_inorder()

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        return self.tree.__iter__()

    def __getitem__(self, Key):
        """ Retrieves the item in the table with the specified key. """
        return self.tree.retrieve(Key)

    @property
    def key_map(self):
        """ Gets the mapping function that maps list items to their search keys. """
        return self.tree.key_map

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        return self.tree.count

    @property
    def is_empty(self):
        """ Gets a boolean value that indicates whether the table is empty or not. """
        return self.tree.is_empty


class Hashtable(ITable):
    """ Represents a hash table that uses separate chaining. """

    def __init__(self, KeyMap, BucketFactory):
        """ Creates a new hash table, from the provided key map and the bucket table factory. """
        self.bucket_count = 0
        self.prime_list = [31, 97, 389, 1543, 6151, 24593, 98317, 393241, 1572869, 6291469, 25165843, 100663319, 402653189, 1610612741, 4294967291]
        self.key_map = KeyMap
        self.bucket_factory = BucketFactory
        self.buckets = [None] * self.prime_list[0]

    def get_next_prime(self):
        """ Gets the next prime in the prime list. If this prime is not available, -1 is returned. """
        i = 0
        while i < len(self.prime_list) - 1:
            if self.prime_list[i] > self.bucket_capacity:
                return self.prime_list[i]
            i += 1
        return -1

    def resize_table(self):
        """ Tries to resize the table to the next prime and re-hashes every element. """
        nextPrime = self.get_next_prime()
        if nextPrime > -1:
            oldBuckets = self.buckets
            self.buckets = [None] * nextPrime
            self.bucket_count = 0
            for i in range(len(oldBuckets)):
                if oldBuckets[i] is not None:
                    for item in oldBuckets[i]:
                        self.insert(item)

    def insert(self, Item):
        """ Inserts an item in the hash table. """
        key = self.key_map.map(Item)
        hashCode = hash(key)
        bucket = self.get_new_bucket(hashCode)
        if self.bucket_contains_key(bucket, key):
            return False
        bucket.insert(Item)
        if self.bucket_load_factor > 0.66:
            self.resize_table()
        return True

    def get_new_bucket(self, HashCode):
        """ Gets the bucket for items with the given hash code or creates a new one, if necessary. """
        index = HashCode % self.bucket_capacity
        bucket = self.buckets[index]
        if bucket is None:
            self.bucket_count += 1
            bucket = self.bucket_factory.create(self.key_map)
            self.buckets[index] = bucket
        return bucket

    def bucket_contains_key(self, Bucket, Key):
        """ Finds out if a bucket contains the given key. """
        return self.find_in_bucket(Bucket, Key) is not None

    def find_in_bucket(self, Bucket, Key):
        """ Finds an item in the given bucket with the given key. """
        if Bucket is None:
            return None
        return Bucket[Key]

    def get_bucket(self, HashCode):
        """ Gets the bucket for items with the given hash code. """
        index = HashCode % self.bucket_capacity
        bucket = self.buckets[index]
        return bucket

    def delete_bucket(self, HashCode):
        """ Deletes the bucket with the provided hash code. """
        index = HashCode % self.bucket_capacity
        self.buckets[index] = None
        self.bucket_count -= 1

    def contains_key(self, Key):
        """ Gets a boolean value that indicates if the hash table contains the given key. """
        return self.bucket_contains_key(self.get_bucket(hash(Key)), Key)

    def remove(self, Key):
        """ Removes a key from the table. """
        hashCode = hash(Key)
        bucket = self.get_bucket(hashCode)
        if bucket is None:
            return False
        result = bucket.remove(Key)
        if bucket.count == 0:
            self.delete_bucket(hashCode)
        return result

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        for i in range(len(self.buckets)):
            if self.buckets[i] is not None:
                for item in self.buckets[i]:
                    yield item

    def to_list(self):
        """ Gets the table's items as a read-only list. """
        results = ArrayList()
        for i in range(len(self.buckets)):
            if self.buckets[i] is not None:
                for item in self.buckets[i]:
                    results.add(item)
        return results

    @property
    def bucket_capacity(self):
        """ Gets the number of buckets in the table. """
        return len(self.buckets)

    @property
    def key_map(self):
        """ Gets the record-to-key mapping function used by this hash table. """
        return self.key_map_value

    @key_map.setter
    def key_map(self, value):
        """ Sets the record-to-key mapping function used by this hash table. """
        self.key_map_value = value

    @property
    def bucket_factory(self):
        """ Gets the factory that is used to create new buckets for this hash table. """
        return self.bucket_factory_value

    @bucket_factory.setter
    def bucket_factory(self, value):
        """ Sets the factory that is used to create new buckets for this hash table. """
        self.bucket_factory_value = value

    @property
    def bucket_load_factor(self):
        """ Gets the bucket load factor. """
        return self.bucket_count / self.bucket_capacity

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        result = 0
        for i in range(len(self.buckets)):
            if self.buckets[i] is not None:
                result += self.buckets[i].count
        return result

    @property
    def is_empty(self):
        for i in range(len(self.buckets)):
            if self.buckets[i] is not None and self.buckets[i].count > 0:
                return False
        return True

    def __getitem__(self, Key):
        """ Retrieves the item in the table with the specified key. """
        bucket = self.get_bucket(hash(Key))
        return self.find_in_bucket(bucket, Key)


class OpenHashtableItem:
    """ Describes an item in an open hash table. """

    def __init__(self, Value, IsEmpty):
        """ Creates a new item for use in an open hash table. """
        self.value = Value
        self.is_empty = IsEmpty


class OpenHashtable(ITable):
    """ Represents a hash table that uses open addressing. """

    def __init__(self, KeyMap, ProbeSequenceMap):
        """ Creates a new hash table, with the provided key map and probe sequence map. """
        self.prime_list = [31, 97, 389, 1543, 6151, 24593, 98317, 393241, 1572869, 6291469, 25165843, 100663319, 402653189, 1610612741, 4294967291]
        self.values = [None] * self.prime_list[0]
        self.count_value = 0
        self.key_map = KeyMap
        self.probe_sequence_map = ProbeSequenceMap

    def get_next_prime(self):
        """ Gets the next prime in the prime list. If this prime is not available, -1 is returned. """
        i = 0
        while i < len(self.prime_list) - 1:
            if self.prime_list[i] > self.capacity:
                return self.prime_list[i]
            i += 1
        return -1

    def resize_table(self):
        """ Tries to resize the table to the next prime and re-hashes every element. """
        nextPrime = self.get_next_prime()
        if nextPrime > -1:
            oldValues = self.values
            self.values = [None] * nextPrime
            self.count = 0
            for i in range(len(oldValues)):
                if oldValues[i] is not None and (not oldValues[i].is_empty):
                    self.insert(oldValues[i].value)

    def insert(self, Item):
        """ Inserts an item into the table. """
        key = self.key_map.map(Item)
        openItem = self.find_open_item(key)
        if openItem is None:
            return False
        else:
            self.count += 1
            openItem.value = Item
            openItem.is_empty = False
            if self.load_factor > 0.75:
                self.resize_table()
            return True

    def find_open_item(self, Key):
        hashCode = hash(Key)
        seq = self.probe_sequence_map.map(hashCode)
        for index in seq:
            i = index % self.capacity
            if self.is_open(i):
                if self.values[i] is None:
                    self.values[i] = OpenHashtableItem(None, True)
                return self.values[i]
            elif self.key_map.map(self.values[i].value) == Key:
                return None

    def is_open(self, Index):
        """ Finds out if the position at the given index is open. """
        return self.values[Index] is None or self.values[Index].is_empty

    def contains_key(self, Key):
        """ Finds out if the table contains the specified key. """
        return self[Key] is not None

    def remove(self, Key):
        """ Removes a key from the table. """
        hashCode = hash(Key)
        seq = self.probe_sequence_map.map(hashCode)
        for index in seq:
            i = index % self.capacity
            if self.values[i] is None:
                return False
            elif (not self.values[i].is_empty) and self.key_map.map(self.values[i].value) == Key:
                self.values[i].is_empty = True
                self.count -= 1
                return True

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        for i in range(len(self.values)):
            if self.values[i] is not None and (not self.values[i].is_empty):
                yield self.values[i].value

    def to_list(self):
        """ Gets the table's items as a read-only list. """
        results = ArrayList()
        for i in range(len(self.values)):
            if self.values[i] is not None and (not self.values[i].is_empty):
                results.add(self.values[i].value)
        return results

    @property
    def capacity(self):
        """ Gets the table's capacity. """
        return len(self.values)

    @property
    def count(self):
        """ Gets the number of items in the table. """
        return self.count_value

    @count.setter
    def count(self, value):
        """ Sets the number of items in the table. """
        self.count_value = value

    @property
    def key_map(self):
        """ Gets the record-to-key mapping function used by this hash table. """
        return self.key_map_value

    @key_map.setter
    def key_map(self, value):
        """ Sets the record-to-key mapping function used by this hash table. """
        self.key_map_value = value

    @property
    def probe_sequence_map(self):
        """ Gets the open addressed hash table's hash key to probe sequence mapping function. """
        return self.probe_sequence_map_value

    @probe_sequence_map.setter
    def probe_sequence_map(self, value):
        """ Sets the open addressed hash table's hash key to probe sequence mapping function. """
        self.probe_sequence_map_value = value

    @property
    def load_factor(self):
        """ Gets the table's load factor. """
        return self.count / self.capacity

    def __getitem__(self, Key):
        """ Retrieves the item in the table with the specified key. """
        hashCode = hash(Key)
        seq = self.probe_sequence_map.map(hashCode)
        for index in seq:
            i = index % self.capacity
            if self.values[i] is None:
                return None
            elif (not self.values[i].is_empty) and self.key_map.map(self.values[i].value) == Key:
                return self.values[i].value

    @property
    def is_empty(self):
        """ Gets a boolean value that indicates whether the table is empty or not. """
        return self.count == 0


class SwapTable(ITable):
    """ A wrapper table that allows for the underlying table to be 'swapped'. """

    def __init__(self, table):
        """ Creates a new instance of a swap table. """
        self.table = table

    def swap(self, Table):
        """ Changes the underlying table implementation to the provided table. """
        for item in self:
            Table.insert(item)
        self.table = Table

    def insert(self, Value):
        """ Inserts an item into the table. """
        return self.table.insert(Value)

    def contains_key(self, Key):
        """ Finds out if the table contains the specified key. """
        return self.table.contains_key(Key)

    def remove(self, Key):
        """ Removes a key from the table. """
        return self.table.remove(Key)

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        return self.table.__iter__()

    def to_list(self):
        """ Gets the table's items as a read-only list. """
        return self.table.to_list()

    def __getitem__(self, Key):
        """ Retrieves the item in the table with the specified key. """
        return self.table[Key]

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        return self.table.count


class PowerSequenceMap(IMap):
    """ Describes a mapping function that maps an integer to a probe sequence of the specified order. """

    def __init__(self, Power):
        """ Creates a new instance of a power sequence map based on the power to which the offsets will be exponentiated. """
        self.power = Power

    def map(self, Value):
        """ Maps the starting point of a sequence to a power sequence with offsets exponentiated to the value exposed by the 'Power' property. """
        index = 0
        while True:
            offset = index
            i = 1
            while i < self.power:
                offset *= index
                i += 1
            yield Value + offset
            index += 1

    @property
    def power(self):
        """ Gets the power to which the offsets will be exponentiated. """
        return self.power_value

    @power.setter
    def power(self, value):
        """ Sets the power to which the offsets will be exponentiated. """
        self.power_value = value


class BinaryTreeTableFactory(IFactory):
    """ A factory that creates empty binary search tree tables from a value-key mapping function. """

    def __init__(self):
        pass

    def create(self, Argument):
        """ Creates a new instance from the provided argument. """
        return TreeTable(BinarySearchTree(Argument))


class DefaultRecordMap(IMap):
    """ A mapping function that simply passes along the search key provided by the record. """

    def __init__(self):
        pass

    def map(self, Record):
        """ Maps the item to its target representation. """
        return Record.key