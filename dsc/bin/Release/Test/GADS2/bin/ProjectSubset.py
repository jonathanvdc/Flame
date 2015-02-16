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
    """ Describes a generic read-only list. """

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


class ISortedList(ICollection):
    """ Describes a modified version of the Sorted List ADT. """

    def remove(self, Item):
        """ Removes an item from the list. """
        raise NotImplementedError("Method 'ISortedList.remove' was not implemented.")

    def contains(self, Item):
        """ Finds out if the sorted list contains the given item. """
        raise NotImplementedError("Method 'ISortedList.contains' was not implemented.")

    def to_list(self):
        """ Returns a read-only list that represents this list's contents, for easy enumeration. """
        raise NotImplementedError("Method 'ISortedList.to_list' was not implemented.")

    @property
    def is_empty(self):
        """ Gets a boolean value that indicates if the sorted list is empty. """
        raise NotImplementedError("Getter of property 'ISortedList.is_empty' was not implemented.")


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
        """ Gets a boolean value that indicates if the list is empty. """
        raise NotImplementedError("Getter of property 'ITree.is_empty' was not implemented.")


class ArrayList(IList):
    """ An array-based implementation of a list. """

    def __init__(self, data = None):
        """ Creates a new instance of a list backed by the provided array. """
        if data is None:
            self.data = [None] * 5
            self.elem_count = 0
            return
        self.data = [None] * 5
        self.elem_count = 0
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
        list = self.data
        i = len(list)
        j = 0
        k = len(Target)
        while j < i and j < k:
            Target[j] = list[j]
            j += 1

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
        self.value_value = None
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


class Stack(IReadOnlyCollection):
    """ Represents a generic stack. """

    def __init__(self, dataContainer = None):
        """ Creates a new stack instance that uses the specified list to store its data. """
        if dataContainer is None:
            self.data_container = None
            self.data_container = LinkedList()
            return
        self.data_container = None
        self.data_container = dataContainer

    def push(self, Item):
        """ Pushes an item on the stack. """
        self.data_container.insert(0, Item)

    def pop(self):
        """ Pops the item at the top of the stack. """
        if self.is_empty:
            return None
        value = self.data_container[0]
        self.data_container.remove_at(0)
        return value

    @property
    def is_empty(self):
        """ Gets a boolean value that indicates whether the stack is empty or not. """
        return self.count == 0

    @property
    def count(self):
        """ Gets the number of items on the stack. """
        return self.data_container.count

    @property
    def top(self):
        """ Peeks at the item at the top of the stack, without removing it. """
        if self.is_empty:
            return None
        else:
            return self.data_container[0]


class Queue(IReadOnlyCollection):
    """ Represents a generic queue. """

    def __init__(self, dataContainer = None):
        """ Creates a new queue instance that uses the specified list to store its data. """
        if dataContainer is None:
            self.data_container = None
            self.data_container = ArrayList()
            return
        self.data_container = None
        self.data_container = dataContainer

    def enqueue(self, Item):
        """ Adds an item to the queue. """
        self.data_container.add(Item)

    def dequeue(self):
        """ Dequeues an item and returns it. """
        if self.is_empty:
            return None
        value = self.data_container[0]
        self.data_container.remove_at(0)
        return value

    @property
    def is_empty(self):
        """ Gets a boolean value that indicates whether the queue is empty or not. """
        return self.count == 0

    @property
    def count(self):
        """ Gets the number of items on the queue. """
        return self.data_container.count

    @property
    def front(self):
        """ Peeks at the item at the top of the queue, without removing it. """
        if self.is_empty:
            return None
        else:
            return self.data_container[0]


class BinaryTree:
    """ A link-based implementation of the Binary Tree ADT. """

    def __init__(self, Data):
        """ Creates a new binary tree from a data item. """
        self.data_value = None
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
        if tree is None:
            self.tree = None
            self.key_map_value = None
            self.key_map = KeyMap
            return
        self.tree = None
        self.key_map_value = None
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
            yield rolist
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
        self.tree = None
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


class TreeSortedList(ISortedList):
    """ Describes a tree implementation of a sorted list. """

    def __init__(self, tree):
        """ Creates a new search tree implementation of a sorted list, using the provided tree as backing storage. """
        self.tree = None
        self.tree = tree

    def add(self, Item):
        """ Adds an item to the collection. """
        self.tree.insert(Item)

    def remove(self, Item):
        """ Removes an item from the list. """
        return self.tree.remove(self.key_map.map(Item))

    def contains(self, Item):
        """ Finds out if the sorted list contains the given item. """
        return self.tree.retrieve(self.key_map.map(Item)) is not None

    def to_list(self):
        """ Returns a read-only list that represents this list's contents, for easy enumeration. """
        return self.tree.traverse_inorder()

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        return self.tree.__iter__()

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
        """ Gets a boolean value that indicates whether the sorted list is empty or not. """
        return self.tree.is_empty


class DefaultRecordMap(IMap):
    """ A mapping function that simply passes along the search key provided by the record. """

    def map(self, Record):
        """ Maps the item to its target representation. """
        return Record.key