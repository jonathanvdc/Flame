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
        # Pre:
        # Other must either be a 'BinaryTree<T>' or 'None'.
        # Post:
        # If the given tree is not 'None', the Data, Left and Right properties are copied from the target tree into this tree.
        # Note that these copies are shallow: the left and right trees (and possibly Data) will be mere aliases to the information contained in the other tree.
        # If the provided other tree is 'None', Data, Left and Right are all set to 'None'.
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