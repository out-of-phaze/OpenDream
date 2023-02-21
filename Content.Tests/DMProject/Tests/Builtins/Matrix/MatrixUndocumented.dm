//Below are some tests to make sure our impl of the undocumented /matrix() signatures actually works.
// Issue OD#1055: https://github.com/OpenDreamProject/OpenDream/issues/1055

/proc/RunTest()
	//MATRIX_COPY
	var/matrix/doReMe = matrix(1,2,3,4,5,6)
	var/matrix/copyMatrix = matrix(doReMe,MATRIX_COPY)
	if(doReMe ~! copyMatrix)
		CRASH("MATRIX_COPY failed to copy a matrix, got [copyMatrix] instead.")
	if(doReMe == copyMatrix)
		CRASH("MATRIX_COPY failed to copy a matrix, got the same matrix back instead.")
	
	//MATRIX_SCALE
	ASSERT(matrix(2,MATRIX_SCALE) ~= matrix(2,0,0,0,2,0))
	ASSERT(matrix(2,3,MATRIX_SCALE) ~= matrix(2,0,0,0,3,0))

	//MATRIX_TRANSLATE
	ASSERT(matrix(2,MATRIX_TRANSLATE) ~= matrix(1,0,2,0,1,2)) // completely undocumented, but, whatever.
	ASSERT(matrix(2,3,MATRIX_TRANSLATE) ~= matrix(1,0,2,0,1,3))
	ASSERT(matrix(doReMe,2,3,MATRIX_TRANSLATE) ~= matrix(1,2,5,4,5,9))
	if(doReMe ~! matrix(1,2,3,4,5,6))
		CRASH("MATRIX_TRANSLATE modified the matrix it was given, without being given a MATRIX_MODIFY flag.")

	//MATRIX_ROTATE
	var/matrix/rotated = matrix(90, MATRIX_ROTATE)
	//TODO: Fix discrepancy in our trigonometry results
	rotated.a = round(rotated.a)
	rotated.b = round(rotated.b)
	rotated.c = round(rotated.c)
	rotated.d = round(rotated.d)
	rotated.e = round(rotated.e)
	rotated.f = round(rotated.f)
	if(rotated ~! matrix(0,1,0,-1,0,0))
		CRASH("MATRIX_ROTATE failure, expected \[0,1,0,-1,0,0\], got [json_encode(matrix(90, MATRIX_ROTATE))]")
	
	//MATRIX_INVERT
	ASSERT(matrix(doReMe,MATRIX_INVERT) ~= matrix(-1.66666667, 0.6666667, 1, 1.3333334, -0.33333334, -2))

	//MATRIX_MODIFY
	matrix(doReMe,MATRIX_INVERT | MATRIX_MODIFY)
	if(doReMe ~! matrix(-1.66666667, 0.6666667, 1, 1.3333334, -0.33333334, -2))
		CRASH("MATRIX_INVERT | MATRIX_MODIFY failed to leave a modified, inverted matrix. Matrix is instead [doReMe].")
