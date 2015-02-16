from LinkedList import *
from BinaryTree import *
from BinarySearchTree import *
from ITree import *

class BinarySearchTree(ITree):
    """ Describes a binary search tree. """

    def __init__(self, KeyMap, tree = None):
        if tree is None:
            self.tree = None
            self.key_map = KeyMap
            return
        self.key_map = KeyMap
        self.tree = tree

    def traverse_inorder(self, Target = None):
        """ Performs inorder traversal on the binary search tree and writes its items to the given target collection. """
        # Post:
        # This method returns a read-only list with element type 'T' that can be used for iteration.
        if Target is None:
            aList = LinkedList()
            self.traverse_inorder(aList)
            return aList
        if self.tree is not None:
            self.tree.traverse_inorder(Target)

    def get_leftmost(self):
        """ Gets the binary tree's leftmost node.
            This method is private. """
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
        # Post:
        # The item will be added to the tree, regardless of whether the tree already contains an item with the same search key.
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
        # Post:
        # If the binary search tree contains an item with the specified key, said item is returned.
        # If not, None is returned.
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
        # Post:
        # If the search tree contains an item with the provided key, it is removed, and true is returned.
        # Otherwise, the tree's state remains unchanged, and false is returned.
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
        """ Returns a tree where one occurrence of an item with the provided key has been removed.
            Search trees are treated as immutable objects in this method.
            This method is private. """
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
        """ Returns a tree with the leftmost child removed.
            Search trees are treated as immutable objects in this method.
            This method is private. """
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
        """ Compares two trees for equality.
            This method is not intended for general use, and was specifically created for the 'Remove(TKey)' method.
            It uses reference comparison to achieve O(log(n)) performance.
            This method is private. """
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
        """ Sets the binary tree's left subtree.
            This accessor is private. """
        self.tree.left = value.tree

    @property
    def key_map(self):
        """ Gets the function that maps the binary search tree's records to their search keys. """
        return self.key_map_value

    @key_map.setter
    def key_map(self, value):
        """ Sets the function that maps the binary search tree's records to their search keys.
            This accessor is private. """
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
        """ Sets the binary tree's right subtree.
            This accessor is private. """
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
        # Post:
        # Returns true if this binary search tree is empty or has no children.
        # Otherwise, returns false.
        return self.tree is None or self.tree.left is None and self.tree.right is None